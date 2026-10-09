using UnityEngine;

using UnityEditor;

// 저장된 게임 진행 상황(돈, 도감, 업그레이드, 최고 기록, 바다 등록, 물 구멍 위치)을 모두 지우는 메뉴.
// 플레이 중에는 메모리에 남은 진행 상황이 다시 저장되므로 멈춘 상태에서만 쓸 수 있다.
public static class ProgressResetTool
{
    /// <summary>
    /// 확인을 받은 뒤 이 프로젝트의 PlayerPrefs를 모두 지운다.
    /// 입력값은 없으며, 저장된 PlayerPrefs를 변경한다.
    /// </summary>
    [MenuItem("Tools/Reset Progress")]
    public static void ResetProgress()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "진행 상황 초기화",
            "돈, 도감, 업그레이드, 최고 기록, 바다 등록, 물 구멍 위치를 모두 지웁니다.\n되돌릴 수 없습니다.",
            "초기화",
            "취소");
        if (!confirmed) return;

        // 진행 상황 키는 여러 클래스에 흩어져 있고 PlayerPrefs는 키 목록을 주지 않아 전부 지운다.
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("진행 상황을 초기화했습니다.");
    }

    /// <summary>
    /// 플레이 중이 아닐 때만 초기화 메뉴를 켠다.
    /// 에디터 플레이 상태를 사용하며, 메뉴 활성 여부를 반환한다.
    /// </summary>
    [MenuItem("Tools/Reset Progress", true)]
    public static bool CanResetProgress()
    {
        return !EditorApplication.isPlaying;
    }
}
