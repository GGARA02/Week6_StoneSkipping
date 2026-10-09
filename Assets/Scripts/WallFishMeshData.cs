using System;

using UnityEngine;

[Serializable]
public sealed class WallFishMeshData
{
    [SerializeField] private Vector3[] _vertices;
    [SerializeField] private Vector3[] _normals;
    [SerializeField] private Vector2[] _uv;
    [SerializeField] private int[] _triangles;

    /// <summary>
    /// mesh와 scale을 사용해 마지막 접촉 조각을 손에 들 크기로 복사한다.
    /// 벽의 비균일 스케일과 비율을 보존하고 긴 높이 축을 투척물의 길이 축으로 돌려 저장한다.
    /// </summary>
    public WallFishMeshData(Mesh mesh, Vector3 scale)
    {
        _vertices = mesh.vertices;
        _normals = mesh.normals;
        _uv = mesh.uv;
        _triangles = mesh.triangles;
        Vector3 size = Vector3.Scale(mesh.bounds.size, scale);
        float factor = 3f / Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < _vertices.Length; i++)
        {
            _vertices[i] = rotation * Vector3.Scale(_vertices[i] - mesh.bounds.center, scale) * factor;
            _normals[i] = rotation * new Vector3(_normals[i].x / scale.x, _normals[i].y / scale.y, _normals[i].z / scale.z).normalized;
        }
    }

    /// <summary>
    /// 저장된 정점, 법선, UV와 삼각형을 사용해 독립된 Wall 메시를 반환한다.
    /// 저장 데이터는 변경하지 않으며 반환 메시의 수명은 호출자가 관리한다.
    /// </summary>
    public Mesh ToMesh()
    {
        Mesh mesh = new Mesh { name = "Wall Fish" };
        mesh.vertices = _vertices;
        mesh.normals = _normals;
        mesh.uv = _uv;
        mesh.triangles = _triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
