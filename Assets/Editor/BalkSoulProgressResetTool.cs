using UnityEngine;

using UnityEditor;

public static class BalkSoulProgressResetTool
{
    private const string MENU_PATH = "Tools/BalkSoul/Reset Acquisition";
    private const string ACQUISITION_KEY = "SkipStoneV2.Fish.balksoul";
    private const string SEA_KEY = "SkipStoneV2.Sea.balksoul";

    /// <summary>
    /// 입력값 없이 볼크소울의 획득 기록과 바다 출현 해금 기록을 삭제하고 PlayerPrefs에 저장한다.
    /// 다음 플레이에서 W02Ball 진화 연출과 재획득을 다시 볼 수 있게 한다.
    /// </summary>
    [MenuItem(MENU_PATH)]
    private static void ResetAcquisition()
    {
        if (!CanResetAcquisition()) return;

        PlayerPrefs.DeleteKey(ACQUISITION_KEY);
        PlayerPrefs.DeleteKey(SEA_KEY);
        PlayerPrefs.Save();
        Debug.Log("볼크소울 획득 상태를 초기화했습니다.");
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
