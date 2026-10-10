using UnityEngine;

// 수면 아래에서 기다렸다가 포물선으로 튀어 오르는 물고기(또는 게) 한 마리. 물고기 프리팹의 루트에 붙는다.
// 던진 물고기에 맞거나 다시 물에 들어가면 스포너에 알린다.
public class Fish : MonoBehaviour
{
    private const float WIGGLE_SPEED = 14f;
    private const float WIGGLE_ANGLE = 12f;
    private const float TIRE_ROLL_SPEED = 360f;
    private const float CAN_FLIP_SPEED = 620f;

    [Header("종류")]
    [SerializeField]
    private FishType _type;

    [Header("참조")]
    [Tooltip("메시를 가진 자식 몸 오브젝트. 복어는 이 오브젝트를 부풀린다")]
    [SerializeField]
    private Transform _body;

    [Header("상태")]
    private FishSpawner _spawner;
    private Vector3 _startPosition;
    private Vector3 _position;
    private Vector3 _velocity;
    private float _gravity;
    private float _delay;
    private float _airTime;
    private float _waterY;
    private bool _jumped;
    private bool _caught;
    private bool _isWall;
    private Mesh _wallMesh;
    private bool _isEjected;
    private FisherQteController _fisherQte;

    public FishType Type => _type;
    public Transform Body => _body;
    public bool IsEjected => _isEjected;

    void Update()
    {
        if (_isWall || Time.timeScale == 0f) return;
        if (!_jumped)
        {
            _delay -= Time.deltaTime;
            if (_delay > 0f) return;

            _jumped = true;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
            }
            _spawner.HandleFishSurfaced(_startPosition);
        }

        float dt = Time.deltaTime;
        _airTime += dt;
        _velocity += Vector3.down * (_gravity * dt);
        _position += _velocity * dt;
        transform.SetPositionAndRotation(_position, BodyRotation());

        if (_velocity.y < 0f && _position.y < _waterY - 0.5f)
        {
            _spawner.HandleFishLanded(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isEjected || _caught || !_jumped || Time.timeScale == 0f || other.attachedRigidbody != _spawner.PlayerBody) return;

        if (_fisherQte != null)
        {
            if (_fisherQte.TryBegin()) _caught = true;
            return;
        }

        _caught = true;
        _spawner.HandleFishCaught(this);
    }

    void OnDestroy()
    {
        if (_wallMesh != null) Destroy(_wallMesh);
    }

    /// <summary>
    /// spawner, type와 소유할 mesh로 절단된 벽을 포획 가능한 물고기로 준비한다.
    /// 점프 이동은 끄고 접촉 시 도감 등록과 마지막 메쉬 저장을 허용한다.
    /// </summary>
    public void InitializeWall(FishSpawner spawner, FishType type, Mesh mesh)
    {
        _spawner = spawner;
        _type = type;
        _body = transform;
        _isWall = true;
        _jumped = true;
        _wallMesh = mesh;
    }

    /// <summary>
    /// 입력값 없이 이미지 패널을 SCREEN 선택 템플릿으로 초기화한다.
    /// 종류와 몸 참조를 설정하고 자연 출현용 이동을 끈다.
    /// </summary>
    public void InitializeScreen()
    {
        _type = FishType.CreateScreen();
        _body = transform;
        enabled = false;
    }

    /// <summary>
    /// delay 뒤 start에서 launchVelocity로 튀어 오르게 준비하고, 튀어 오르기 전까지 몸을 숨긴다.
    /// 각 인자를 사용하며, 이동 상태와 몸 표시 여부를 변경한다.
    /// </summary>
    public void Launch(FishSpawner spawner, Vector3 start, Vector3 launchVelocity, float delay, float waterY)
    {
        _spawner = spawner;
        _startPosition = start;
        _position = start;
        _velocity = launchVelocity;
        _delay = delay;
        _waterY = waterY;
        _gravity = -Physics.gravity.y;
        _airTime = 0f;
        _jumped = false;
        _caught = false;
        _isEjected = false;
        transform.position = start;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }
        if (TryGetComponent(out _fisherQte)) _fisherQte.Initialize(this, spawner);
    }

    /// <summary>
    /// 공중의 start에서 launchVelocity로 즉시 사출하고 입수 전까지 포획을 막는다.
    /// spawner와 waterY를 사용하며, 표시 상태와 사출 이동 상태를 초기화한다.
    /// </summary>
    public void Eject(FishSpawner spawner, Vector3 start, Vector3 launchVelocity, float waterY)
    {
        Launch(spawner, start, launchVelocity, 0f, waterY);
        _jumped = true;
        _isEjected = true;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }
    }

    /// <summary>
    /// 날아가는 동안의 몸 방향을 구한다. 머리가 진행 방향을 보며 몸을 흔들고, 타이어와 캔은 굴러가거나 뒤집힌다.
    /// _velocity, _airTime, 종류 모양을 사용하며, 회전을 반환한다.
    /// </summary>
    private Quaternion BodyRotation()
    {
        if (_fisherQte != null && !_isEjected)
            return _fisherQte.IsResolving ? transform.rotation : Quaternion.identity;

        if (Type.Shape == FishShape.Tire)
        {
            // 굴러가듯 세워서 돌면서 좌우로 털썩털썩 흔들린다.
            return Quaternion.Euler(_airTime * TIRE_ROLL_SPEED, Mathf.Sin(_airTime * 9f) * 25f, 90f + Mathf.Sin(_airTime * 13f) * 30f);
        }
        if (Type.Shape == FishShape.Can)
        {
            // 앞뒤로 뒤집히면서 불규칙하게 파닥인다.
            return Quaternion.Euler(_airTime * CAN_FLIP_SPEED, Mathf.Sin(_airTime * 11f) * 40f, Mathf.Sin(_airTime * 17f) * 35f);
        }

        Quaternion wiggle = Quaternion.Euler(0f, Mathf.Sin(_airTime * WIGGLE_SPEED) * WIGGLE_ANGLE, 0f);
        return Quaternion.LookRotation(_velocity) * wiggle;
    }
}
