using UnityEngine;

public sealed class CuttableWall : MonoBehaviour
{
    private const float MIN_CUT_FRACTION = 0.02f;
    private const float EJECTION_SPEED = 12f;
    private const float UPWARD_SPEED = 4f;

    [Header("상태")]
    private FishSpawner _spawner;
    private bool _cut;
    private Mesh _remainingMesh;

    void OnDestroy()
    {
        if (_remainingMesh != null) Destroy(_remainingMesh);
    }

    /// <summary>
    /// spawner를 사용해 절단 조각을 등록할 물고기 스포너를 저장한다.
    /// 벽의 렌더링과 충돌 상태는 변경하지 않는다.
    /// </summary>
    public void Initialize(FishSpawner spawner)
    {
        _spawner = spawner;
    }

    /// <summary>
    /// 월드 접촉점 point와 절단면 normal로 벽을 두 개의 밀폐된 조각으로 나눈다.
    /// 위 조각만 움직이는 Wall 물고기로 분리하고 아래 조각은 원본의 고정 장애물로 유지한다.
    /// </summary>
    public bool Cut(Vector3 point, Vector3 normal)
    {
        if (_cut || normal.sqrMagnitude < 0.000001f) return false;

        normal.Normalize();
        if (Mathf.Abs(normal.y) < 0.0001f) normal = Vector3.up;
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3 localPoint = transform.InverseTransformPoint(point);
        Vector3 localNormal = transform.localToWorldMatrix.transpose.MultiplyVector(normal).normalized;
        float minProjection = float.PositiveInfinity;
        float maxProjection = float.NegativeInfinity;
        foreach (Vector3 vertex in mesh.vertices)
        {
            float projection = Vector3.Dot(localNormal, vertex);
            minProjection = Mathf.Min(minProjection, projection);
            maxProjection = Mathf.Max(maxProjection, projection);
        }

        // 모서리 접촉에서는 축별 보정 대신 절단면 방향으로 여유를 확보해 부피 없는 조각을 피한다.
        float inset = (maxProjection - minProjection) * MIN_CUT_FRACTION;
        float pointProjection = Vector3.Dot(localNormal, localPoint);
        float cutProjection = Mathf.Clamp(pointProjection, minProjection + inset, maxProjection - inset);
        localPoint += localNormal * (cutProjection - pointProjection);
        if (!WallMeshCutter.Slice(mesh, new Plane(localNormal, localPoint), out Mesh positive, out Mesh negative)) return false;

        _cut = true;
        bool positiveIsUpper = GetWorldCentroid(positive).y >= GetWorldCentroid(negative).y;
        Mesh upper = positiveIsUpper ? positive : negative;
        _remainingMesh = positiveIsUpper ? negative : positive;
        CreatePiece(upper, positiveIsUpper ? normal : -normal);

        GetComponent<MeshFilter>().sharedMesh = _remainingMesh;
        Collider originalCollider = GetComponent<Collider>();
        originalCollider.enabled = false;
        Destroy(originalCollider);
        MeshCollider remainingCollider = gameObject.AddComponent<MeshCollider>();
        remainingCollider.convex = true;
        remainingCollider.sharedMesh = _remainingMesh;
        return true;
    }

    /// <summary>
    /// 밀폐된 mesh의 삼각형 부피를 사용해 월드 좌표의 무게중심을 반환한다.
    /// 원본 변환을 적용하여 기울어진 절단에서도 위쪽 조각을 판정한다.
    /// </summary>
    private Vector3 GetWorldCentroid(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float volume = 0f;
        Vector3 weightedCenter = Vector3.zero;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            float tetrahedron = Vector3.Dot(a, Vector3.Cross(b, c));
            volume += tetrahedron;
            weightedCenter += (a + b + c) * (tetrahedron * 0.25f);
        }
        return transform.TransformPoint(weightedCenter / volume);
    }

    /// <summary>
    /// mesh와 separation 방향을 사용해 위 조각을 중력과 분리 속도를 가진 Wall 물고기로 만든다.
    /// 원본 머티리얼과 월드 크기를 유지하며 접촉과 메쉬 수명은 Fish가 관리한다.
    /// </summary>
    private void CreatePiece(Mesh mesh, Vector3 separation)
    {
        Vector3 center = mesh.bounds.center;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i] -= center;
        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        GameObject piece = new GameObject("Wall") { layer = gameObject.layer };
        piece.transform.SetParent(transform.parent, false);
        piece.transform.localScale = transform.localScale;
        piece.transform.rotation = transform.rotation;
        piece.transform.position = transform.TransformPoint(center) + separation * 0.04f;
        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = piece.AddComponent<MeshRenderer>();
        MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterials = sourceRenderer.sharedMaterials;
        renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
        renderer.receiveShadows = sourceRenderer.receiveShadows;
        MeshCollider collider = piece.AddComponent<MeshCollider>();
        collider.convex = true;
        collider.sharedMesh = mesh;
        collider.isTrigger = true;
        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        piece.AddComponent<Fish>().InitializeWall(_spawner, _spawner.WallFish.Type, mesh);
        body.AddForce(separation * EJECTION_SPEED + Vector3.up * UPWARD_SPEED, ForceMode.VelocityChange);
    }
}
