using UnityEngine;

// 수면 아래에서 기다렸다가 포물선으로 튀어 오르는 물고기(또는 게) 한 마리. 물고기 프리팹의 루트에 붙는다.
// 던진 물고기에 맞거나 다시 물에 들어가면 스포너에 알린다.
public class Fish : MonoBehaviour
{
    private const float WIGGLE_SPEED = 14f;
    private const float WIGGLE_ANGLE = 12f;
    private const float CRAB_SPIN_SPEED = 420f;
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

    public FishType Type => _type;
    public Transform Body => _body;

    void Update()
    {
        if (_isWall) return;
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
        if (_caught || !_jumped || other.attachedRigidbody != _spawner.PlayerBody) return;

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
        transform.position = start;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }
    }

    /// <summary>
    /// 날아가는 동안의 몸 방향을 구한다. 물고기는 머리가 진행 방향을 보며 몸을 흔들고, 게는 납작하게 빙글빙글 돈다.
    /// _velocity, _airTime, 종류 모양을 사용하며, 회전을 반환한다.
    /// </summary>
    private Quaternion BodyRotation()
    {
        if (Type.Shape == FishShape.Crab)
        {
            return Quaternion.Euler(Mathf.Sin(_airTime * 6f) * 20f, _airTime * CRAB_SPIN_SPEED, 0f);
        }
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
