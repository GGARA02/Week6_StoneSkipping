using System.Collections.Generic;

using UnityEngine;

using UnityEditor;

// 프로젝트의 모든 물고기를 도감에 등록해 던질 수 있게 하는 메뉴.
// 물고기 종류는 프리팹의 Fish 컴포넌트에서 찾고, 런타임에 만들어지는 Wall과 SCREEN도 함께 등록한다.
public static class UnlockAllFishTool
{
    private const string FISH_KEY_PREFIX = "SkipStoneV2.Fish.";
    private const string TYPE_ID_PATH = "_type._id";
    private static readonly string[] RUNTIME_FISH_IDS = { "wall", "screen" };

    /// <summary>
    /// 모든 프리팹의 Fish 종류와 런타임 생성 종류를 도감에 등록한다.
    /// 입력값은 없으며, 찾은 종류마다 PlayerPrefs의 도감 키를 저장한다.
    /// </summary>
    [MenuItem("Tools/Unlock All Fish")]
    public static void UnlockAllFish()
    {
        HashSet<string> ids = CollectFishIds();
        foreach (string id in ids)
        {
            PlayerPrefs.SetInt(FISH_KEY_PREFIX + id, 1);
        }
        PlayerPrefs.Save();
        Debug.Log($"물고기 {ids.Count}종을 도감에 등록했습니다: {string.Join(", ", ids)}");
    }

    /// <summary>
    /// 프로젝트 전체 프리팹에서 Fish 컴포넌트의 종류 ID를 모은다.
    /// 입력값은 없으며, 런타임 생성 종류를 포함한 중복 없는 ID 집합을 반환한다.
    /// </summary>
    private static HashSet<string> CollectFishIds()
    {
        HashSet<string> ids = new HashSet<string>(RUNTIME_FISH_IDS);
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab == null) continue;

            foreach (Fish fish in prefab.GetComponentsInChildren<Fish>(true))
            {
                string id = new SerializedObject(fish).FindProperty(TYPE_ID_PATH)?.stringValue;
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
        }
        return ids;
    }
}
