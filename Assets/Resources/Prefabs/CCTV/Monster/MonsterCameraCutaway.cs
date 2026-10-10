using System.Collections;

using UnityEngine;

public class MonsterCameraCutaway : MonoBehaviour
{
    private const float CAMERA_CUTAWAY_DURATION = 0.5f;

    private PlayerController _playerController;
    private Coroutine _cutawayCoroutine;
    private Camera _monsterCamera;
    private Camera _mainCamera;
    private AudioListener _mainAudioListener;
    private bool _mainCameraWasEnabled;
    private bool _mainAudioListenerWasEnabled;

    void OnEnable()
    {
        _playerController = GetComponentInParent<PlayerController>();
        if (_playerController != null)
        {
            _playerController.OnJudge += HandleJudge;
            _playerController.OnSlidePush += HandleSlidePush;
        }
    }

    void OnDisable()
    {
        if (_playerController != null)
        {
            _playerController.OnJudge -= HandleJudge;
            _playerController.OnSlidePush -= HandleSlidePush;
        }
        _playerController = null;
        RestoreCameras();
    }

    /// <summary>
    /// 부모 플레이어의 성공 판정을 받으면 몬스터 카메라 연출을 재생한다.
    /// judge를 사용하고 timingError는 사용하지 않으며, Perfect 또는 Good일 때만 Camera와 AudioListener의 enabled 상태 및 코루틴을 변경한다.
    /// </summary>
    private void HandleJudge(SkipJudge judge, float timingError)
    {
        if (judge == SkipJudge.Perfect || judge == SkipJudge.Good)
        {
            Play();
        }
    }

    /// <summary>
    /// 부모 플레이어의 슬라이드 밀기 입력을 받으면 몬스터 카메라 연출을 재생한다.
    /// 입력값 없이 Play를 호출하며, Camera와 AudioListener의 enabled 상태 및 코루틴을 변경한다.
    /// </summary>
    private void HandleSlidePush()
    {
        Play();
    }

    /// <summary>
    /// 이전 연출을 복구한 뒤 몬스터 카메라 연출을 새로 시작한다.
    /// 입력값 없이 비활성 자식 카메라와 Camera.main을 사용하며, 몬스터 카메라 GameObject와 메인 Camera 및 AudioListener의 enabled 상태를 변경한다.
    /// </summary>
    public void Play()
    {
        RestoreCameras();
        _monsterCamera = GetComponentInChildren<Camera>(true);
        _mainCamera = Camera.main;
        _mainAudioListener = _mainCamera.GetComponent<AudioListener>();
        _mainCameraWasEnabled = _mainCamera.enabled;
        _mainAudioListenerWasEnabled = _mainAudioListener.enabled;
        _monsterCamera.gameObject.SetActive(true);
        _mainCamera.enabled = false;
        _mainAudioListener.enabled = false;
        _cutawayCoroutine = StartCoroutine(PlayCutaway());
    }

    /// <summary>
    /// 몬스터 카메라를 0.5초 동안 보여준 뒤 원래 카메라를 복구한다.
    /// 입력값 없이 연출 시간을 사용하며, 대기용 열거자를 반환하고 Camera와 AudioListener의 기존 enabled 상태를 복구한다.
    /// </summary>
    private IEnumerator PlayCutaway()
    {
        yield return new WaitForSeconds(CAMERA_CUTAWAY_DURATION);
        _cutawayCoroutine = null;
        RestoreCameras();
    }

    /// <summary>
    /// 진행 중인 연출을 중단하고 몬스터 카메라를 숨긴 뒤 메인 카메라와 오디오 리스너 컴포넌트를 복구한다.
    /// 입력값 없이 저장된 코루틴과 컴포넌트를 사용하며, 기존 enabled 상태를 복구하고 관련 참조와 상태를 초기화한다.
    /// </summary>
    private void RestoreCameras()
    {
        if (_cutawayCoroutine != null)
        {
            StopCoroutine(_cutawayCoroutine);
            _cutawayCoroutine = null;
        }
        if (_monsterCamera != null)
        {
            _monsterCamera.gameObject.SetActive(false);
            _monsterCamera = null;
        }
        if (_mainCamera != null)
        {
            _mainCamera.enabled = _mainCameraWasEnabled;
            _mainCamera = null;
        }
        if (_mainAudioListener != null)
        {
            _mainAudioListener.enabled = _mainAudioListenerWasEnabled;
            _mainAudioListener = null;
        }
        _mainCameraWasEnabled = false;
        _mainAudioListenerWasEnabled = false;
    }
}
