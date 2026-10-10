using System;

using UnityEngine;
using UnityEngine.InputSystem;

public class EndingCreditUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("엔딩 크레딧 표시 지속 시간(초)")]
    private const float DISPLAYDURATION = 30f;

    private float _timer;

    public event Action OnEndingCompleted;

    void OnEnable()
    {
        _timer = 0f;
    }

    void Update()
    {
        HandleSkipInput();
        UpdateTimer();
    }

    /// <summary>
    /// 엔딩 크레딧을 종료하고 UI를 비활성화한 뒤 완료 이벤트를 발생시킨다.
    /// 입력값은 없으며, gameObject의 활성화 상태를 변경하고 OnEndingCompleted 이벤트를 호출한다.
    /// </summary>
    public void FinishCredits()
    {
        gameObject.SetActive(false);
        OnEndingCompleted?.Invoke();
    }

    /// <summary>
    /// 외부 버튼 클릭 등에서 크레딧을 즉시 스킵할 때 호출한다.
    /// 입력값은 없으며, FinishCredits를 호출한다.
    /// </summary>
    public void SkipButton()
    {
        FinishCredits();
    }

    /// <summary>
    /// 스페이스바 또는 패드 버튼 입력을 받아 크레딧을 즉시 종료한다.
    /// Keyboard와 Gamepad 입력을 사용하며, 스킵 조건 만족 시 FinishCredits를 호출한다.
    /// </summary>
    private void HandleSkipInput()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        bool skipPressed = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);

        if (skipPressed)
        {
            FinishCredits();
        }
    }

    /// <summary>
    /// 지속 시간을 측정하여 설정된 시간이 지나면 크레딧을 종료한다.
    /// _displayDuration과 Time.unscaledDeltaTime을 사용하며, 시간 도달 시 FinishCredits를 호출한다.
    /// </summary>
    private void UpdateTimer()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer >= DISPLAYDURATION)
        {
            FinishCredits();
        }
    }
}
