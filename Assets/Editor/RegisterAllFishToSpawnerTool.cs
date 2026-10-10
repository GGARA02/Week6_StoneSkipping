using System.Collections.Generic;

using UnityEngine;

using UnityEditor;
using UnityEditor.SceneManagement;

// 프로젝트의 모든 물고기 프리팹을 씬의 FishSpawner 물고기 목록에 등록하는 메뉴.
// 루트에 Fish 컴포넌트가 있는 프리팹만 대상이며, 같은 종류 ID가 이미 목록에 있으면 건너뛴다.
public static class RegisterAllFishToSpawnerTool
{
    private const string FISH_PREFABS_PATH = "_fishPrefabs";
    private const string TYPE_ID_PATH = "_type._id";
    private const string PREFERRED_FOLDER = "Assets/Resources/Prefabs/Fish/";

    /// <summary>
    /// 모든 물고기 프리팹 중 목록에 없는 종류를 현재 씬 FishSpawner의 _fishPrefabs 끝에 추가한다.
    /// 입력값은 없으며, 스포너 목록과 씬 변경 상태를 갱신한다. 씬 저장은 사용자가 직접 한다.
    /// </summary>
    [MenuItem("Tools/Register All Fish To Spawner")]
    public static void RegisterAllFish()
    {
        FishSpawner spawner = Object.FindFirstObjectByType<FishSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("현재 씬에서 FishSpawner를 찾지 못했습니다.");
            return;
        }

        SerializedObject spawnerObject = new SerializedObject(spawner);
        SerializedProperty list = spawnerObject.FindProperty(FISH_PREFABS_PATH);

        HashSet<string> registeredIds = new HashSet<string>();
        for (int i = 0; i < list.arraySize; i++)
        {
            Fish existing = list.GetArrayElementAtIndex(i).objectReferenceValue as Fish;
            if (existing != null) registeredIds.Add(GetTypeId(existing));
        }

        List<string> added = new List<string>();
        foreach (Fish prefab in FindFishPrefabs())
        {
            string id = GetTypeId(prefab);
            if (string.IsNullOrEmpty(id) || !registeredIds.Add(id)) continue;

            int index = list.arraySize;
            list.arraySize = index + 1;
            list.GetArrayElementAtIndex(index).objectReferenceValue = prefab;
            added.Add(id);
        }

        spawnerObject.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Debug.Log(added.Count == 0
            ? "추가할 물고기가 없습니다. 모든 종류가 이미 등록되어 있습니다."
            : $"스포너에 물고기 {added.Count}종을 등록했습니다: {string.Join(", ", added)}. 씬을 저장하세요.");
    }

    /// <summary>
    /// 플레이 중이 아닐 때만 등록 메뉴를 켠다.
    /// 에디터 플레이 상태를 사용하며, 메뉴 활성 여부를 반환한다.
    /// </summary>
    [MenuItem("Tools/Register All Fish To Spawner", true)]
    public static bool CanRegisterAllFish()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    /// <summary>
    /// 프로젝트에서 루트에 Fish 컴포넌트가 있는 프리팹을 찾는다.
    /// 입력값은 없으며, 기본 물고기 폴더를 앞에 두어 같은 종류 ID 중 그 프리팹이 우선되도록 정렬해 반환한다.
    /// </summary>
    private static List<Fish> FindFishPrefabs()
    {
        List<string> paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }
        paths.Sort((a, b) =>
        {
            bool aPreferred = a.StartsWith(PREFERRED_FOLDER);
            bool bPreferred = b.StartsWith(PREFERRED_FOLDER);
            return aPreferred != bPreferred
                ? (aPreferred ? -1 : 1)
                : string.CompareOrdinal(a, b);
        });

        List<Fish> result = new List<Fish>();
        foreach (string path in paths)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root != null && root.TryGetComponent(out Fish fish)) result.Add(fish);
        }
        return result;
    }

    /// <summary>
    /// fish의 직렬화된 종류 ID를 읽는다.
    /// 입력값은 fish이며, 종류 ID 문자열을 반환한다.
    /// </summary>
    private static string GetTypeId(Fish fish)
    {
        return new SerializedObject(fish).FindProperty(TYPE_ID_PATH)?.stringValue;
    }
}
