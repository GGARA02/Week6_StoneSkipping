using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Splits readable, closed convex wall meshes in local space without changing the source.
/// Exterior normals, UV0 and material submeshes are retained; flat caps use submesh zero.
/// </summary>
public static class WallMeshCutter
{
    private const float RELATIVE_EPSILON = 0.000001f;

    /// <summary>
    /// Clips source against localPlane and returns two owned meshes with outward flat caps.
    /// Positive is on the normal side. Returns false with null outputs for invalid inputs,
    /// non-triangle topology, tangential contact or cuts below the mesh-relative tolerance.
    /// The caller owns both output meshes and must destroy them when no longer needed.
    /// </summary>
    public static bool Slice(Mesh source, Plane localPlane, out Mesh positive, out Mesh negative)
    {
        positive = null;
        negative = null;
        if (source == null || !source.isReadable || source.vertexCount < 4 || source.subMeshCount == 0)
            return false;

        float normalLength = localPlane.normal.magnitude;
        if (!IsFinite(normalLength) || normalLength <= 0f || !IsFinite(localPlane.distance))
            return false;

        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            if (source.GetTopology(submesh) != MeshTopology.Triangles)
                return false;
        }

        Plane plane = new Plane(localPlane.normal / normalLength, localPlane.distance / normalLength);
        Vector3[] positions = source.vertices;
        Vector3[] normals = source.normals;
        Vector2[] uvs = source.uv;
        Bounds bounds = new Bounds(positions[0], Vector3.zero);
        for (int index = 0; index < positions.Length; index++)
        {
            Vector3 position = positions[index];
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
                return false;
            bounds.Encapsulate(position);
        }

        float scale = bounds.size.magnitude;
        if (!IsFinite(scale) || scale <= 0f)
            return false;

        float epsilon = scale * RELATIVE_EPSILON;
        var boundary = new List<Vector3>();
        var vertices = new Vertex[positions.Length];
        bool hasPositive = false;
        bool hasNegative = false;
        for (int index = 0; index < positions.Length; index++)
        {
            float distance = plane.GetDistanceToPoint(positions[index]);
            Vector3 position = positions[index];
            if (Mathf.Abs(distance) <= epsilon)
            {
                position -= plane.normal * distance;
                distance = 0f;
            }
            hasPositive |= distance > 0f;
            hasNegative |= distance < 0f;
            vertices[index] = new Vertex(position,
                normals.Length == positions.Length ? normals[index] : Vector3.zero,
                uvs.Length == positions.Length ? uvs[index] : Vector2.zero, distance);
        }

        if (!hasPositive || !hasNegative)
            return false;

        var positiveBuilder = new MeshBuilder(source.subMeshCount);
        var negativeBuilder = new MeshBuilder(source.subMeshCount);
        var triangle = new Vertex[3];
        var polygon = new List<Vertex>(4);
        var triangles = new List<int>();
        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            source.GetTriangles(triangles, submesh);
            for (int index = 0; index < triangles.Count; index += 3)
            {
                for (int corner = 0; corner < 3; corner++)
                {
                    Vertex vertex = vertices[triangles[index + corner]];
                    if (vertex.Distance == 0f)
                        vertex = new Vertex(Weld(vertex.Position, boundary, epsilon), vertex.Normal, vertex.Uv, 0f);
                    triangle[corner] = vertex;
                }

                if (normals.Length != positions.Length)
                {
                    Vector3 faceNormal = Vector3.Cross(triangle[1].Position - triangle[0].Position,
                        triangle[2].Position - triangle[0].Position).normalized;
                    for (int corner = 0; corner < 3; corner++)
                    {
                        Vertex vertex = triangle[corner];
                        triangle[corner] = new Vertex(vertex.Position, faceNormal, vertex.Uv, vertex.Distance);
                    }
                }

                Clip(triangle, true, polygon, boundary, plane, epsilon);
                positiveBuilder.AddPolygon(polygon, submesh);
                Clip(triangle, false, polygon, boundary, plane, epsilon);
                negativeBuilder.AddPolygon(polygon, submesh);
            }
        }

        if (boundary.Count < 3)
            return false;

        Vector3 center = Vector3.zero;
        foreach (Vector3 point in boundary)
            center += point - bounds.center;
        center = bounds.center + center / boundary.Count;
        center -= plane.normal * plane.GetDistanceToPoint(center);
        Vector3 axisU = Vector3.Cross(plane.normal,
            Mathf.Abs(plane.normal.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
        Vector3 axisV = Vector3.Cross(plane.normal, axisU);
        boundary.Sort((left, right) => GetAngle(left - center, axisU, axisV)
            .CompareTo(GetAngle(right - center, axisU, axisV)));

        float minimumArea = epsilon * epsilon;
        for (int index = 0; index < boundary.Count; index++)
        {
            Vector3 first = boundary[index];
            Vector3 second = boundary[(index + 1) % boundary.Count];
            float area = Vector3.Dot(Vector3.Cross(first - center, second - center), plane.normal);
            if (area <= minimumArea)
                return false;

            positiveBuilder.AddCapTriangle(center, second, first, -plane.normal, axisU, axisV);
            negativeBuilder.AddCapTriangle(center, first, second, plane.normal, axisU, axisV);
        }

        double minimumVolume = (double)epsilon * scale * scale;
        if (positiveBuilder.GetVolume(bounds.center) <= minimumVolume ||
            negativeBuilder.GetVolume(bounds.center) <= minimumVolume)
            return false;

        positive = positiveBuilder.ToMesh(source.name + "_Positive");
        negative = negativeBuilder.ToMesh(source.name + "_Negative");
        return true;
    }

    /// <summary>
    /// Returns whether value is finite so invalid external mesh or plane data is rejected.
    /// </summary>
    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>
    /// Returns point's angle in the axisU/axisV basis for convex boundary ordering.
    /// </summary>
    private static float GetAngle(Vector3 point, Vector3 axisU, Vector3 axisV)
    {
        return Mathf.Atan2(Vector3.Dot(point, axisV), Vector3.Dot(point, axisU));
    }

    /// <summary>
    /// Reuses a boundary point within epsilon of position, or stores and returns position.
    /// </summary>
    private static Vector3 Weld(Vector3 position, List<Vector3> boundary, float epsilon)
    {
        float squaredEpsilon = epsilon * epsilon;
        foreach (Vector3 point in boundary)
        {
            if ((point - position).sqrMagnitude <= squaredEpsilon)
                return point;
        }
        boundary.Add(position);
        return position;
    }

    /// <summary>
    /// Clips triangle to the requested half-space into polygon, preserving its winding.
    /// Intersections use plane and epsilon to weld shared boundary positions.
    /// </summary>
    private static void Clip(Vertex[] triangle, bool positiveSide, List<Vertex> polygon,
        List<Vector3> boundary, Plane plane, float epsilon)
    {
        polygon.Clear();
        for (int index = 0; index < triangle.Length; index++)
        {
            Vertex current = triangle[index];
            Vertex next = triangle[(index + 1) % triangle.Length];
            bool currentInside = positiveSide ? current.Distance >= 0f : current.Distance <= 0f;
            if (currentInside)
                polygon.Add(current);

            if ((current.Distance > 0f && next.Distance < 0f) ||
                (current.Distance < 0f && next.Distance > 0f))
            {
                Vertex intersection = Interpolate(current, next, plane, boundary, epsilon);
                polygon.Add(intersection);
            }
        }
    }

    /// <summary>
    /// Interpolates the crossing edge's position, normal and UV using signed distances.
    /// Returns a plane-projected vertex with a welded position and zero distance.
    /// </summary>
    private static Vertex Interpolate(Vertex first, Vertex second, Plane plane,
        List<Vector3> boundary, float epsilon)
    {
        if (first.Distance < second.Distance)
        {
            Vertex temporary = first;
            first = second;
            second = temporary;
        }
        float fraction = first.Distance / (first.Distance - second.Distance);
        Vector3 position = Vector3.LerpUnclamped(first.Position, second.Position, fraction);
        position -= plane.normal * plane.GetDistanceToPoint(position);
        position = Weld(position, boundary, epsilon);
        return new Vertex(position, Vector3.LerpUnclamped(first.Normal, second.Normal, fraction).normalized,
            Vector2.LerpUnclamped(first.Uv, second.Uv, fraction), 0f);
    }

    private readonly struct Vertex
    {
        private readonly Vector3 _position;
        private readonly Vector3 _normal;
        private readonly Vector2 _uv;
        private readonly float _distance;

        public Vector3 Position => _position;
        public Vector3 Normal => _normal;
        public Vector2 Uv => _uv;
        public float Distance => _distance;

        /// <summary>
        /// Stores position, normal, uv and signed distance for an immutable clipping vertex.
        /// </summary>
        public Vertex(Vector3 position, Vector3 normal, Vector2 uv, float distance)
        {
            _position = position;
            _normal = normal;
            _uv = uv;
            _distance = distance;
        }
    }

    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int>[] _submeshes;

        /// <summary>
        /// Allocates triangle lists for submeshCount original material slots.
        /// </summary>
        public MeshBuilder(int submeshCount)
        {
            _submeshes = new List<int>[submeshCount];
            for (int index = 0; index < submeshCount; index++)
                _submeshes[index] = new List<int>();
        }

        /// <summary>
        /// Adds a winding-preserving fan of polygon vertices to the specified submesh.
        /// Degenerate triangles do not enter the output buffers.
        /// </summary>
        public void AddPolygon(List<Vertex> polygon, int submesh)
        {
            for (int index = 1; index + 1 < polygon.Count; index++)
                AddTriangle(polygon[0], polygon[index], polygon[index + 1], submesh);
        }

        /// <summary>
        /// Adds first/second/third as a flat cap triangle in material slot zero.
        /// Uses normal for hard shading and axisU/axisV for local-unit planar UVs.
        /// </summary>
        public void AddCapTriangle(Vector3 first, Vector3 second, Vector3 third, Vector3 normal,
            Vector3 axisU, Vector3 axisV)
        {
            AddTriangle(CreateCapVertex(first, normal, axisU, axisV),
                CreateCapVertex(second, normal, axisU, axisV),
                CreateCapVertex(third, normal, axisU, axisV), 0);
        }

        /// <summary>
        /// Returns a vertex at position with the cap normal and basis-projected UVs.
        /// </summary>
        private static Vertex CreateCapVertex(Vector3 position, Vector3 normal, Vector3 axisU, Vector3 axisV)
        {
            return new Vertex(position, normal,
                new Vector2(Vector3.Dot(position, axisU), Vector3.Dot(position, axisV)), 0f);
        }

        /// <summary>
        /// Appends the nonzero-area first/second/third triangle and its attributes to submesh.
        /// Separate render vertices retain original normal and UV seams.
        /// </summary>
        private void AddTriangle(Vertex first, Vertex second, Vertex third, int submesh)
        {
            if (Vector3.Cross(second.Position - first.Position, third.Position - first.Position).sqrMagnitude <= 0f)
                return;

            int start = _positions.Count;
            AddVertex(first);
            AddVertex(second);
            AddVertex(third);
            _submeshes[submesh].Add(start);
            _submeshes[submesh].Add(start + 1);
            _submeshes[submesh].Add(start + 2);
        }

        /// <summary>
        /// Appends vertex's position, normal and UV to corresponding output buffers.
        /// </summary>
        private void AddVertex(Vertex vertex)
        {
            _positions.Add(vertex.Position);
            _normals.Add(vertex.Normal);
            _uvs.Add(vertex.Uv);
        }

        /// <summary>
        /// Returns absolute enclosed volume using reference-relative signed tetrahedra.
        /// The reference reduces cancellation for meshes away from the local origin.
        /// </summary>
        public double GetVolume(Vector3 reference)
        {
            double volume = 0d;
            foreach (List<int> triangles in _submeshes)
            {
                for (int index = 0; index < triangles.Count; index += 3)
                {
                    Vector3 first = _positions[triangles[index]] - reference;
                    Vector3 second = _positions[triangles[index + 1]] - reference;
                    Vector3 third = _positions[triangles[index + 2]] - reference;
                    volume += ((double)first.x * ((double)second.y * third.z - (double)second.z * third.y)
                        + (double)first.y * ((double)second.z * third.x - (double)second.x * third.z)
                        + (double)first.z * ((double)second.x * third.y - (double)second.y * third.x)) / 6d;
                }
            }
            return Math.Abs(volume);
        }

        /// <summary>
        /// Returns a new named mesh from the buffers, retaining all original submesh slots.
        /// Uses 32-bit indices when needed and recalculates bounds without changing normals.
        /// </summary>
        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_positions.Count > ushort.MaxValue)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.subMeshCount = _submeshes.Length;
            for (int index = 0; index < _submeshes.Length; index++)
                mesh.SetTriangles(_submeshes[index], index, false);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
