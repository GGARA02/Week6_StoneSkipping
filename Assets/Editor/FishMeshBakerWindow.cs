using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using UnityEditor;

public class FishMeshBakerWindow : EditorWindow
{
    private const string DEFAULT_MESH_PATH = "Assets/Resources/Mesh/Fish";

    private GameObject _targetObject;
    private string _saveFolder = DEFAULT_MESH_PATH;
    private string _meshName = "";
    private bool _createBakedPrefab = true;
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Fish Mesh Baker")]
    public static void OpenWindow()
    {
        FishMeshBakerWindow window = GetWindow<FishMeshBakerWindow>("Fish Mesh Baker");
        window.minSize = new Vector2(380f, 320f);
        window.Show();
    }

    [MenuItem("GameObject/Bake Fish Mesh", false, 10)]
    public static void OpenWithSelectedGameObject(MenuCommand command)
    {
        FishMeshBakerWindow window = GetWindow<FishMeshBakerWindow>("Fish Mesh Baker");
        window.minSize = new Vector2(380f, 320f);
        if (command.context is GameObject go)
        {
            window.SetTarget(go);
        }
        window.Show();
    }

    void OnEnable()
    {
        if (Selection.activeGameObject != null && _targetObject == null)
        {
            SetTarget(Selection.activeGameObject);
        }
    }

    void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            SetTarget(Selection.activeGameObject);
            Repaint();
        }
    }

    void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("물고기 / 복합 프리팹 메시 베이커", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("여러 개의 기본 도형(Cylinder, Cube 등)이나 하위 메시로 구성된 프리팹을 하나의 읽기 가능한 단일 .asset 메시로 구워냅니다.", MessageType.Info);
        EditorGUILayout.Space(8f);

        EditorGUI.BeginChangeCheck();
        GameObject newTarget = (GameObject)EditorGUILayout.ObjectField("대상 오브젝트 / 프리팹", _targetObject, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck())
        {
            SetTarget(newTarget);
        }

        EditorGUILayout.Space(4f);
        _meshName = EditorGUILayout.TextField("생성할 메시 이름", _meshName);
        _saveFolder = EditorGUILayout.TextField("저장 폴더 경로", _saveFolder);
        _createBakedPrefab = EditorGUILayout.Toggle("단일 메시 프리팹 함께 생성", _createBakedPrefab);

        EditorGUILayout.Space(12f);
        EditorGUI.BeginDisabledGroup(_targetObject == null || string.IsNullOrWhiteSpace(_meshName));
        if (GUILayout.Button("메시 굽기 (Bake Mesh)", GUILayout.Height(36f)))
        {
            BakeCurrentTarget();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 대상을 설정하고 기본 메시 이름을 설정한다.
    /// target을 사용하며, _targetObject와 _meshName을 변경한다.
    /// </summary>
    public void SetTarget(GameObject target)
    {
        _targetObject = target;
        if (target != null && string.IsNullOrWhiteSpace(_meshName))
        {
            _meshName = target.name;
        }
    }

    /// <summary>
    /// 현재 설정된 대상의 하위 메시들을 구워 .asset 에셋으로 저장하고 필요 시 단일 메시 프리팹을 생성한다.
    /// _targetObject, _saveFolder, _meshName을 사용하며, 에셋 파일과 프리팹을 생성한다.
    /// </summary>
    private void BakeCurrentTarget()
    {
        if (_targetObject == null) return;

        // 인스턴스를 임시 생성하여 로컬 변환을 정확히 계산한다.
        GameObject instance = Instantiate(_targetObject);
        instance.name = _targetObject.name;
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        try
        {
            Transform bodyTransform = FindBodyTransform(instance.transform);
            Mesh bakedMesh = CombineHierarchyMeshes(instance.transform, bodyTransform, out Material[] materials);
            if (bakedMesh == null)
            {
                EditorUtility.DisplayDialog("오류", "구울 수 있는 유효한 메시를 찾지 못했습니다.", "확인");
                return;
            }

            EnsureDirectoryExists(_saveFolder);
            string assetPath = Path.Combine(_saveFolder, $"{_meshName}.asset").Replace('\\', '/');
            AssetDatabase.CreateAsset(bakedMesh, assetPath);
            AssetDatabase.SaveAssets();

            if (_createBakedPrefab)
            {
                CreateBakedPrefabAsset(instance, bakedMesh, materials, assetPath);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", $"성공적으로 메시를 구웠습니다!\n저장 경로: {assetPath}", "확인");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        }
        finally
        {
            DestroyImmediate(instance);
        }
    }

    /// <summary>
    /// 프리팹 내 Body 트랜스폼을 찾거나 없으면 루트를 반환한다.
    /// root를 사용하며, 기준이 될 Transform을 반환한다.
    /// </summary>
    private Transform FindBodyTransform(Transform root)
    {
        Fish fish = root.GetComponent<Fish>();
        if (fish != null && fish.Body != null)
        {
            return fish.Body;
        }

        Transform body = root.Find("Body");
        return body != null ? body : root;
    }

    /// <summary>
    /// 하위 트랜스폼의 모든 일반 메시와 스킨드 메시를 머티리얼별로 결합하여 단일 복합 메시를 생성한다.
    /// root와 referenceFrame을 사용하며, 결합된 Mesh와 머티리얼 배열을 반환한다.
    /// </summary>
    private Mesh CombineHierarchyMeshes(Transform root, Transform referenceFrame, out Material[] orderedMaterials)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        if (filters.Length == 0 && skinnedRenderers.Length == 0)
        {
            orderedMaterials = Array.Empty<Material>();
            return null;
        }

        Dictionary<Material, List<CombineInstance>> materialGroups = new Dictionary<Material, List<CombineInstance>>();
        Matrix4x4 refWorldToLocal = referenceFrame.worldToLocalMatrix;
        List<Mesh> tempMeshesToDestroy = new List<Mesh>();

        // 일반 MeshFilter 처리
        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null) continue;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            Material[] mats = renderer != null ? renderer.sharedMaterials : Array.Empty<Material>();

            Matrix4x4 localToRef = refWorldToLocal * filter.transform.localToWorldMatrix;
            Mesh sourceMesh = CreateReadableMeshCopy(filter.sharedMesh);
            tempMeshesToDestroy.Add(sourceMesh);

            for (int subIndex = 0; subIndex < sourceMesh.subMeshCount; subIndex++)
            {
                Material mat = subIndex < mats.Length && mats[subIndex] != null
                    ? mats[subIndex]
                    : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

                if (!materialGroups.TryGetValue(mat, out List<CombineInstance> combines))
                {
                    combines = new List<CombineInstance>();
                    materialGroups.Add(mat, combines);
                }

                combines.Add(new CombineInstance
                {
                    mesh = sourceMesh,
                    subMeshIndex = subIndex,
                    transform = localToRef,
                });
            }
        }

        // SkinnedMeshRenderer 처리 (BakeMesh)
        foreach (SkinnedMeshRenderer skinned in skinnedRenderers)
        {
            if (skinned.sharedMesh == null) continue;

            Mesh baked = new Mesh();
            skinned.BakeMesh(baked, true);
            tempMeshesToDestroy.Add(baked);

            Material[] mats = skinned.sharedMaterials;
            Matrix4x4 localToRef = refWorldToLocal * skinned.transform.localToWorldMatrix;

            for (int subIndex = 0; subIndex < baked.subMeshCount; subIndex++)
            {
                Material mat = subIndex < mats.Length && mats[subIndex] != null
                    ? mats[subIndex]
                    : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

                if (!materialGroups.TryGetValue(mat, out List<CombineInstance> combines))
                {
                    combines = new List<CombineInstance>();
                    materialGroups.Add(mat, combines);
                }

                combines.Add(new CombineInstance
                {
                    mesh = baked,
                    subMeshIndex = subIndex,
                    transform = localToRef,
                });
            }
        }

        if (materialGroups.Count == 0)
        {
            foreach (Mesh m in tempMeshesToDestroy) DestroyImmediate(m);
            orderedMaterials = Array.Empty<Material>();
            return null;
        }

        List<Material> matList = new List<Material>();
        List<CombineInstance> submeshCombines = new List<CombineInstance>();

        foreach (var pair in materialGroups)
        {
            Mesh submeshCombined = new Mesh();
            submeshCombined.CombineMeshes(pair.Value.ToArray(), true, true);
            tempMeshesToDestroy.Add(submeshCombined);

            submeshCombines.Add(new CombineInstance
            {
                mesh = submeshCombined,
                subMeshIndex = 0,
                transform = Matrix4x4.identity,
            });
            matList.Add(pair.Key);
        }

        Mesh finalMesh = new Mesh
        {
            name = _meshName,
        };
        finalMesh.CombineMeshes(submeshCombines.ToArray(), false, false);
        finalMesh.RecalculateNormals();
        finalMesh.RecalculateTangents();
        finalMesh.RecalculateBounds();

        foreach (Mesh m in tempMeshesToDestroy)
        {
            DestroyImmediate(m);
        }

        orderedMaterials = matList.ToArray();
        return finalMesh;
    }

    /// <summary>
    /// 내장 원시 도형 등 읽기 불가능한 메시도 에디터 환경에서 읽을 수 있는 복사본 메시를 만든다.
    /// source를 사용하며, 읽기 가능한 복제 Mesh를 반환한다.
    /// </summary>
    private Mesh CreateReadableMeshCopy(Mesh source)
    {
        Mesh copy = new Mesh();
        if (source.isReadable)
        {
            copy = Instantiate(source);
            return copy;
        }

        // isReadable이 false인 기본 도형(Cylinder 등)의 경우 기본 CombineMeshes로 복제하여 읽기 가능하게 만든다.
        copy.CombineMeshes(new CombineInstance[]
        {
            new CombineInstance
            {
                mesh = source,
                subMeshIndex = 0,
                transform = Matrix4x4.identity,
            }
        }, true, false);

        return copy;
    }

    /// <summary>
    /// 원본 프리팹의 모든 하위 이펙트(TrailRenderer, ParticleSystem 등)와 컴포넌트를 유지하면서 메시만 단일 구운 메시로 교체한 프리팹을 생성한다.
    /// sourceInstance, bakedMesh, materials, meshAssetPath를 사용하며, .prefab 에셋을 저장한다.
    /// </summary>
    private void CreateBakedPrefabAsset(GameObject sourceInstance, Mesh bakedMesh, Material[] materials, string meshAssetPath)
    {
        string prefabFolder = Path.GetDirectoryName(meshAssetPath);
        string prefabPath = Path.Combine(prefabFolder, $"Prefab_{_meshName}.prefab").Replace('\\', '/');

        // 원본 구조 전체를 복제하여 TrailRenderer, 이펙트, 스크립트 연결을 완벽히 보존한다.
        GameObject bakedRoot = Instantiate(sourceInstance);
        bakedRoot.name = sourceInstance.name;

        // 원본 프리팹의 모든 SkinnedMeshRenderer 및 MeshFilter/MeshRenderer 제거
        // 단, TrailRenderer나 다른 컴포넌트가 붙어 있는 오브젝트는 메시 컴포넌트만 파괴하고 오브젝트는 보존
        SkinnedMeshRenderer[] oldSkinned = bakedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in oldSkinned)
        {
            DestroyImmediate(smr);
        }

        MeshFilter[] oldFilters = bakedRoot.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in oldFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr != null) DestroyImmediate(mr);
            DestroyImmediate(mf);
        }

        // 아무 컴포넌트도 없고 자식도 없는 껍데기 오브젝트들 정리
        CleanEmptyChildren(bakedRoot.transform);

        // Body 오브젝트를 찾거나 생성
        Transform bodyTransform = FindBodyTransform(bakedRoot.transform);
        if (bodyTransform == bakedRoot.transform)
        {
            GameObject bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(bakedRoot.transform, false);
            bodyGO.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            bodyTransform = bodyGO.transform;
        }

        // Body에 단일 구운 메시 장착
        MeshFilter filter = bodyTransform.GetComponent<MeshFilter>();
        if (filter == null) filter = bodyTransform.gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = bakedMesh;

        MeshRenderer renderer = bodyTransform.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = bodyTransform.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;

        // Animator가 있을 경우 비활성화하여 정적 메시 충돌 방지
        Animator animator = bakedRoot.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Fish 컴포넌트 연결 갱신
        Fish fish = bakedRoot.GetComponent<Fish>();
        if (fish != null)
        {
            SerializedObject serializedFish = new SerializedObject(fish);
            SerializedProperty bodyProp = serializedFish.FindProperty("_body");
            if (bodyProp != null)
            {
                bodyProp.objectReferenceValue = bodyTransform;
            }
            SerializedProperty renderersProp = serializedFish.FindProperty("_renderers");
            if (renderersProp != null && renderersProp.isArray)
            {
                renderersProp.ClearArray();
                renderersProp.InsertArrayElementAtIndex(0);
                renderersProp.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            }
            serializedFish.ApplyModifiedProperties();
        }

        PrefabUtility.SaveAsPrefabAsset(bakedRoot, prefabPath);
        DestroyImmediate(bakedRoot);
    }

    /// <summary>
    /// 컴포넌트와 자식이 없는 빈 게임오브젝트를 재귀적으로 정리한다.
    /// parent를 사용하며, 빈 자식 오브젝트들을 제거한다.
    /// </summary>
    private void CleanEmptyChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            CleanEmptyChildren(child);

            if (child.name == "Body") continue;

            Component[] comps = child.GetComponents<Component>();
            if (comps.Length <= 1 && child.childCount == 0)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 지정된 폴더가 없으면 생성한다.
    /// folderPath를 사용하며, 디렉토리를 생성한다.
    /// </summary>
    private void EnsureDirectoryExists(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }
    }
}
