using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 돌 메시와 물고기 프리팹의 메시만 복사하여 회색 반투명 선택 미리보기 텍스처를 보관한다.
/// 게임 동작, 충돌체와 애니메이션은 생성하지 않는다.
/// </summary>
public sealed class FishSelectionPreview : IDisposable
{
    private const int PREVIEW_LAYER = 2;
    private const int TEXTURE_WIDTH = 192;
    private const int TEXTURE_HEIGHT = 128;

    private readonly Dictionary<Fish, RenderTexture> _textures = new Dictionary<Fish, RenderTexture>();
    private readonly GameObject _root;
    private readonly Camera _camera;
    private readonly Material _material;

    public RenderTexture StoneTexture { get; private set; }

    /// <summary>
    /// 입력값 없이 게임 공간에서 떨어진 미리보기 카메라와 반투명 회색 머티리얼을 만든다.
    /// 생성한 카메라와 머티리얼을 미리보기 렌더링 상태에 저장한다.
    /// </summary>
    public FishSelectionPreview()
    {
        _root = new GameObject("Fish Selection Preview") { hideFlags = HideFlags.HideAndDontSave };
        _root.transform.position = new Vector3(10000f, 10000f, 10000f);
        GameObject cameraObject = new GameObject("Preview Camera");
        cameraObject.transform.SetParent(_root.transform, false);
        _camera = cameraObject.AddComponent<Camera>();
        _camera.enabled = false;
        _camera.orthographic = true;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.clear;
        _camera.cullingMask = 1 << PREVIEW_LAYER;
        _camera.nearClipPlane = 0.01f;
        _camera.farClipPlane = 100f;
        _camera.aspect = (float)TEXTURE_WIDTH / TEXTURE_HEIGHT;
        _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _material.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f, 0.55f));
        _material.SetFloat("_Surface", 1f);
        _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        _material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        _material.SetFloat("_ZWrite", 0f);
        _material.SetFloat("_Cull", (float)CullMode.Off);
        _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _material.SetOverrideTag("RenderType", "Transparent");
        _material.renderQueue = (int)RenderQueue.Transparent;
    }

    /// <summary>
    /// prefab을 키로 준비된 회색 미리보기 텍스처를 조회한다.
    /// 보관된 RenderTexture를 반환하며 아직 준비하지 않았다면 null을 반환한다.
    /// </summary>
    public RenderTexture GetTexture(Fish prefab)
    {
        return _textures.TryGetValue(prefab, out RenderTexture texture) ? texture : null;
    }

    /// <summary>
    /// prefab의 메시와 자식 변환을 사용해 회색 미리보기를 한 번 렌더링한다.
    /// OnGUI 밖에서 호출하며, prefab별 투명 배경 RenderTexture를 캐시에 저장한다.
    /// </summary>
    public void PrepareTexture(Fish prefab)
    {
        if (_textures.ContainsKey(prefab)) return;

        GameObject model = new GameObject("Preview Model");
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>())
        {
            AddMesh(model.transform, filter.sharedMesh, prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix);
        }
        foreach (SkinnedMeshRenderer renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            AddMesh(model.transform, renderer.sharedMesh, prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix);
        }

        _textures.Add(prefab, RenderModel(model));
    }

    /// <summary>
    /// stoneMesh를 사용해 현재 돌의 회색 미리보기를 렌더링한다.
    /// 이전 돌 텍스처를 해제하고 새 결과를 StoneTexture에 저장한다.
    /// </summary>
    public void PrepareStoneTexture(Mesh stoneMesh)
    {
        if (StoneTexture != null)
        {
            StoneTexture.Release();
            UnityEngine.Object.Destroy(StoneTexture);
        }
        GameObject model = new GameObject("Stone Preview Model");
        AddMesh(model.transform, stoneMesh, Matrix4x4.identity);
        StoneTexture = RenderModel(model);
    }

    /// <summary>
    /// model의 메시 경계로 크기와 카메라를 맞춰 미리보기를 렌더링한다.
    /// 렌더링 전용 모델을 제거하고 투명 배경 RenderTexture를 반환한다.
    /// </summary>
    private RenderTexture RenderModel(GameObject model)
    {
        model.transform.localRotation = Quaternion.Euler(12f, 65f, 0f);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (i == 0) bounds = renderers[i].bounds;
            else bounds.Encapsulate(renderers[i].bounds);
        }
        float scale = 2f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.01f);
        model.transform.localScale = Vector3.one * scale;
        model.transform.localPosition = -bounds.center * scale;
        model.transform.SetParent(_root.transform, false);
        bounds = new Bounds(_root.transform.position, bounds.size * scale);
        _camera.transform.position = bounds.center - Vector3.forward * (bounds.extents.z + 10f);
        _camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x / _camera.aspect, 0.01f) * 1.2f;
        RenderTexture texture = new RenderTexture(TEXTURE_WIDTH, TEXTURE_HEIGHT, 24, RenderTextureFormat.ARGB32);
        texture.Create();
        _camera.targetTexture = texture;
        _camera.Render();
        _camera.targetTexture = null;
        model.SetActive(false);
        UnityEngine.Object.Destroy(model);
        return texture;
    }

    /// <summary>
    /// mesh와 프리팹 루트 기준 matrix를 사용해 렌더링 전용 자식 메시를 만든다.
    /// parent 아래에 회색 머티리얼을 쓰는 MeshFilter와 MeshRenderer를 추가한다.
    /// </summary>
    private void AddMesh(Transform parent, Mesh mesh, Matrix4x4 matrix)
    {
        GameObject part = new GameObject("Preview Mesh") { layer = PREVIEW_LAYER };
        part.transform.SetParent(parent, false);
        part.transform.localPosition = matrix.GetColumn(3);
        part.transform.localRotation = matrix.rotation;
        part.transform.localScale = matrix.lossyScale;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        Material[] materials = new Material[mesh.subMeshCount];
        for (int i = 0; i < materials.Length; i++) materials[i] = _material;
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    /// <summary>
    /// 입력값 없이 캐시된 텍스처와 카메라, 머티리얼을 해제한다.
    /// 미리보기에서 생성한 Unity 오브젝트를 제거하고 텍스처 캐시를 비운다.
    /// </summary>
    public void Dispose()
    {
        foreach (RenderTexture texture in _textures.Values)
        {
            texture.Release();
            UnityEngine.Object.Destroy(texture);
        }
        _textures.Clear();
        if (StoneTexture != null)
        {
            StoneTexture.Release();
            UnityEngine.Object.Destroy(StoneTexture);
            StoneTexture = null;
        }
        UnityEngine.Object.Destroy(_material);
        UnityEngine.Object.Destroy(_root);
    }
}
