using UnityEngine;

using UnityEditor;

public static class ShipProgressResetTool
{
    private const string MENU_ROOT = "Tools/Ship/Reset Progress/";
    private const string FIRST_THROW_KEY = "SkipStoneV2.ShipNormal.FirstThrow";
    private const string AIDE_SUNK_KEY = "SkipStoneV2.ShipNormal.AideSunk";
    private const string AIDE_SEA_KEY = "SkipStoneV2.Sea.aide";
    private const string AIDE_CAUGHT_KEY = "SkipStoneV2.Fish.aide";
    private const string SHIP_CAUGHT_KEY = "SkipStoneV2.Fish.ship_normal";

    /// <summary>
    /// 입력값 없이 최초 투척과 Aide 기록을 초기화한다.
    /// 배 획득 기록은 유지하여 다음 플레이에서 최초 투척을 다시 시험할 수 있게 한다.
    /// </summary>
    [MenuItem(MENU_ROOT + "First Throw (Keep Ship)")]
    private static void ResetFirstThrow()
    {
        ResetProgress(false);
    }

    /// <summary>
    /// 입력값 없이 배 획득을 포함한 배와 Aide 기록을 초기화한다.
    /// 다음 플레이에서 Placed Ship 획득부터 다시 진행하도록 저장 상태를 변경한다.
    /// </summary>
    [MenuItem(MENU_ROOT + "All Ship and Aide Records")]
    private static void ResetAllShipRecords()
    {
        ResetProgress(true);
    }

    /// <summary>
    /// 현재 에디터 재생 상태를 확인하여 초기화 메뉴의 사용 가능 여부를 반환한다.
    /// 런타임에 보관된 상태가 저장 기록과 달라지지 않도록 재생 중에는 비활성화한다.
    /// </summary>
    [MenuItem(MENU_ROOT + "First Throw (Keep Ship)", true)]
    [MenuItem(MENU_ROOT + "All Ship and Aide Records", true)]
    private static bool CanResetProgress()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    /// <summary>
    /// resetShipAcquisition에 따라 배 획득 기록까지 지울지 결정하고 배 이벤트와 Aide 기록을 삭제한다.
    /// 해당 PlayerPrefs만 저장하며 다른 물고기, 재화, 업그레이드 기록은 유지한다.
    /// </summary>
    private static void ResetProgress(bool resetShipAcquisition)
    {
        if (!CanResetProgress()) return;

        PlayerPrefs.DeleteKey(FIRST_THROW_KEY);
        PlayerPrefs.DeleteKey(AIDE_SUNK_KEY);
        PlayerPrefs.DeleteKey(AIDE_SEA_KEY);
        PlayerPrefs.DeleteKey(AIDE_CAUGHT_KEY);
        if (resetShipAcquisition)
            PlayerPrefs.DeleteKey(SHIP_CAUGHT_KEY);
        PlayerPrefs.Save();

        Debug.Log(resetShipAcquisition
            ? "배와 Aide 기록을 초기화했습니다. 다음 플레이에서 Placed Ship 획득부터 다시 진행하세요."
            : "최초 투척과 Aide 기록을 초기화했습니다. 배 획득 기록은 유지됩니다. 다음 플레이에서 배를 선택해 투척하세요.");
    }
}
