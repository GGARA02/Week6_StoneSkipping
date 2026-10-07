using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private GameObject _water;
    [SerializeField]
    private float _moveForceX;
    [SerializeField]
    private float _moveForceZ;
    [SerializeField]
    private float _moveTorque;
    [SerializeField]
    private float sensitivity;
    [SerializeField]
    private float _startDelay;
    [SerializeField]
    private float _startForce;
    [Header("Bounds")]
    [SerializeField]
    private float _areaPower = 1f;
    [SerializeField]
    private float _boundForce = 1f;
    [SerializeField]
    private float _dragForce = 1f;
    [SerializeField]
    private float _popForce = 1f;
    [SerializeField]
    private float _popPow = 0.5f;

    [Header("모니터링")]

    private InputSystem_Actions _inputActions;
    private Rigidbody _playerRB;
    private Collider _playerCL;
    private bool _isThrowing = false;

    private bool _isGameOver = false;
    private bool _inWater = false;
    private bool _canJump = false;
    private float _gameOverCount = 0;
    private float _waterY;
    private float _startSpeed;

    private bool isFirst = true;
    private bool _jumpRequired = false;
    public System.Action OnGameOver;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        if (_isThrowing && _inputActions.Player.Jump.WasPressedThisFrame() && _canJump && _inWater)
        {
            _jumpRequired = true;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_isThrowing)
        {
            Vector2 moveInput = _inputActions.Player.Move.ReadValue<Vector2>().normalized;
            Vector2 rotationInput = _inputActions.Player.Look.ReadValue<Vector2>().normalized;
            _playerRB.AddForce(Vector3.right * moveInput.x * _moveForceX + Vector3.forward * moveInput.y * _moveForceZ, ForceMode.Acceleration);
            _playerRB.AddTorque(Vector3.right * rotationInput.x * _moveTorque + Vector3.forward * rotationInput.y * _moveTorque, ForceMode.Acceleration);
            float contactArea = Mathf.Clamp01((_waterY - _playerCL.bounds.min.y) / _playerCL.bounds.size.y); //면적비율
            float area = Mathf.Pow(contactArea, _areaPower); // 면적계수
            Vector3 currentDir = _playerRB.linearVelocity.normalized; //현재 진행 방향
            float currentSpeed = _playerRB.linearVelocity.magnitude; //현재 '속도'


            if (_inWater)
            {
                Debug.Log(-currentDir * _dragForce * currentSpeed * currentSpeed * area);
                _playerRB.AddForce(-currentDir * _dragForce * currentSpeed * currentSpeed * area, ForceMode.Acceleration);
            }
            if (_canJump && _jumpRequired)
            {
                Debug.Log("pop");
                _jumpRequired = false;
                _canJump = false;
                _inWater = false;
                Pop();
            }
        }
        if (!_isGameOver && _isThrowing && _playerRB.linearVelocity.sqrMagnitude < 0.01f)
        {
            _gameOverCount += Time.deltaTime;
            if (_gameOverCount >= 3f)
            {
                _isGameOver = true;
                OnGameOver.Invoke();
            }
        }
    }

    public void Initialize(InputSystem_Actions input)
    {
        _inputActions = input;
        _playerRB = GetComponent<Rigidbody>();
        _playerCL = GetComponentInChildren<Collider>();
        _waterY = _water.GetComponent<Collider>().bounds.max.y;
    }

    public void InputDisable()

    {
        _inputActions.Player.Disable();
    }

    public void InputEnable()
    {
        _inputActions.Player.Enable();
    }

    public void ThrowRock(Vector3 throwForce, Vector3 throwTorque)
    {
        _playerRB.useGravity = true;
        _isThrowing = false;
        _playerRB.AddForce(throwForce, ForceMode.Impulse);
        _playerRB.AddTorque(throwTorque, ForceMode.Impulse);
        _isThrowing = true;

        //startWait(_startDelay);
    }

    private IEnumerator startWait(float delay)
    {
        float count = 0;
        while (count < delay)
        {
            yield return null;
        }
        _isThrowing = true;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            if (isFirst)
            {
                isFirst = false;
                _startSpeed = _playerRB.linearVelocity.magnitude;
            }
            Debug.Log("물 진입");
            //타임스케일관리 이벤트 발생?
            // Time.timeScale = 0.5f;
            _canJump = true;
            _inWater = true;
        }
    }

    private void OnTriggerOut(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            //타임스케일 복구 이벤트 발생
            //Time.timeScale = 1.0f;
            _gameOverCount = 0;
            _canJump = false;
            _inWater = false;

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Finish"))
        {
            OnGameOver.Invoke();
        }
    }

    private void Pop()
    {
        float contactArea = Mathf.Clamp01((_waterY - _playerCL.bounds.min.y) / _playerCL.bounds.size.y); //면적비율
        float area = Mathf.Pow(contactArea, _areaPower); // 면적계수
        Vector3 currentDir = _playerRB.linearVelocity.normalized; //현재 진행 방향
        float currentSpeed = new Vector3(_playerRB.linearVelocity.x, 0, _playerRB.linearVelocity.z).magnitude; //현재 '속도'
        float popPower = _popForce * area * Mathf.Pow((currentSpeed / _startSpeed), _popPow);
        Debug.Log(popPower);
        _playerRB.AddForce(popPower * new Vector3(0, 1, 5), ForceMode.Impulse);
    }
}
