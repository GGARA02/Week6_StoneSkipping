using UnityEngine;
using UnityEngine.InputSystem;

// 게임패드를 짧게 진동시키고, 시간이 지나면 끈다. 게임패드가 없으면 아무것도 하지 않는다.
public class RumbleV2 : MonoBehaviour
{
    [Header("진동")]
    [Tooltip("세기 1일 때 저주파(묵직한) 모터 세기")]
    [SerializeField]
    private float _maxLowFrequency = 0.8f;
    [Tooltip("세기 1일 때 고주파(찌릿한) 모터 세기")]
    [SerializeField]
    private float _maxHighFrequency = 1f;
    private float _endTime;
    private float _currentStrength;
    private bool _isRumbling;

    void Update()
    {
        if (_isRumbling && Time.unscaledTime >= _endTime)
        {
            StopMotors();
        }
    }

    void OnDisable()
    {
        StopMotors();
    }

    /// <summary>
    /// 게임패드를 strength 세기로 duration 동안 진동시킨다. 더 약한 진동이 겹치면 지금 진동을 유지한다.
    /// strength(0~1)와 duration(초)을 사용하며, 모터 세기와 종료 시각을 변경한다.
    /// </summary>
    public void Pulse(float strength, float duration)
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return;

        strength = Mathf.Clamp01(strength);
        if (_isRumbling && strength < _currentStrength) return;

        gamepad.SetMotorSpeeds(strength * _maxLowFrequency, strength * _maxHighFrequency);
        _currentStrength = strength;
        _endTime = Time.unscaledTime + duration;
        _isRumbling = true;
    }

    /// <summary>
    /// 진동을 멈춘다.
    /// 입력값은 없으며, 모터 세기와 _isRumbling을 변경한다.
    /// </summary>
    private void StopMotors()
    {
        _isRumbling = false;
        _currentStrength = 0f;
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
    }
}
