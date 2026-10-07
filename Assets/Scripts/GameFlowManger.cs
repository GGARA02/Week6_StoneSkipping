using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManger : MonoBehaviour
{
    [SerializeField]
    private PlayerController _playerController;
    [SerializeField]
    private CameraController2 _cameraController2;

    private InputSystem_Actions _inputActions;

    [SerializeField]
    private Vector3 _throwForce;

    [SerializeField]
    private Vector3 _throwTorque;

    private bool _isThrowing = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        if (_isThrowing == false && _inputActions.UI.Click.WasPressedThisFrame())
        {
            Debug.Log("던져잇");
            ThrowStart();
        }
    }

    private void Initialize()
    {
        _inputActions = new InputSystem_Actions();
        _playerController.Initialize(_inputActions);
        _inputActions.UI.Enable();

        _playerController.OnGameOver += GameOver;
        _cameraController2.GetInputAction(_inputActions);
    }

    private void ThrowStart()
    {
        _isThrowing = true;
        _playerController.InputEnable();
        _playerController.ThrowRock(_throwForce, _throwTorque);
    }

    private void GameOver()
    {
        _playerController.InputDisable();
        Debug.Log("GameOver");
        SceneManager.LoadScene("SampleScene");
    }
}