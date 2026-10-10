using UnityEngine;

using UnityEditor;

public static class AlkagiProgressResetTool
{
    private const string MENU_PATH = "Tools/ALKAGI/Reset Acquisition";
    private const string ACQUISITION_KEY = "SkipStoneV2.Fish.alkagi";

    /// <summary>
    /// 입력값 없이 물고기 ALKAGI의 획득 기록을 삭제하고 PlayerPrefs에 저장한다.
    /// 다음 플레이에서 ALKAGI를 미획득 상태로 되돌려, 외계인을 얻은 상태라면 하늘 ALKAGI가 다시 나타나게 한다.
    /// </summary>
    [MenuItem(MENU_PATH)]
    private static void ResetAcquisition()
    {
        if (!CanResetAcquisition()) return;

        PlayerPrefs.DeleteKey(ACQUISITION_KEY);
        PlayerPrefs.Save();
        Debug.Log("ALKAGI 획득 상태를 초기화했습니다.");
    }

    /// <summary>
    /// 현재 에디터 재생 상태를 사용해 획득 초기화 메뉴의 활성 여부를 반환한다.
    /// 재생 중이거나 재생 모드 전환 중이면 초기화를 차단한다.
    /// </summary>
    [MenuItem(MENU_PATH, true)]
    private static bool CanResetAcquisition()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
