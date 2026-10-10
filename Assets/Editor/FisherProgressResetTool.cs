using UnityEngine;

using UnityEditor;

public static class FisherProgressResetTool
{
    private const string MENU_PATH = "Tools/33Fisher/Reset Acquisition";
    private const string ACQUISITION_KEY = "SkipStoneV2.Fish.33fisher";
    private const string QTE_SUCCESS_KEY = "SkipStoneV2.FisherQteSuccess";

    /// <summary>
    /// 입력값 없이 33Fisher의 습득 기록과 QTE 성공 횟수를 삭제하고 PlayerPrefs에 저장한다.
    /// 다음 플레이에서 바다 출현과 재습득을 허용하며 QTE 난이도를 최초 단계로 되돌린다.
    /// </summary>
    [MenuItem(MENU_PATH)]
    private static void ResetAcquisition()
    {
        if (!CanResetAcquisition()) return;

        PlayerPrefs.DeleteKey(ACQUISITION_KEY);
        PlayerPrefs.DeleteKey(QTE_SUCCESS_KEY);
        PlayerPrefs.Save();
        Debug.Log("33Fisher 습득 상태와 QTE 성공 횟수를 초기화했습니다.");
    }

    /// <summary>
    /// 현재 에디터 재생 상태를 사용해 습득 초기화 메뉴의 활성 여부를 반환한다.
    /// 재생 중이거나 재생 모드 전환 중이면 초기화를 차단한다.
    /// </summary>
    [MenuItem(MENU_PATH, true)]
    private static bool CanResetAcquisition()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
