using UnityEngine;

// 수면 아래에서 기다렸다가 포물선으로 튀어 오르는 물고기(또는 게) 한 마리.
// 돌에 맞거나 다시 물에 들어가면 스포너에 알린다. 복어는 맞으면 먼저 부풀고 나서 잡힌다.
public class FishV2 : MonoBehaviour
{
    private const float WIGGLE_SPEED = 14f;
    private const float WIGGLE_ANGLE = 12f;
    private const float CRAB_SPIN_SPEED = 420f;
    private const float TIRE_ROLL_SPEED = 360f;
    private const float CAN_FLIP_SPEED = 620f;
    private const float INFLATE_TIME = 0.35f;
    private const float INFLATED_GRAVITY_SCALE = 0.3f;

    [Header("상태")]
    private FishSpawnerV2 _spawner;
    private Transform _body;
    private Renderer _bodyRenderer;
    private Vector3 _startPosition;
    private Vector3 _position;
    private Vector3 _velocity;
    private float _gravity;
    private float _delay;
    private float _airTime;
    private float _waterY;
    private float _inflateTimer;
    private bool _jumped;
    private bool _caught;
    private bool _inflating;

    public FishTypeV2 Type { get; private set; }

    void Update()
    {
        if (!_jumped)
        {
            _delay -= Time.deltaTime;
            if (_delay > 0f) return;

            _jumped = true;
            _bodyRenderer.enabled = true;
            _spawner.HandleFishSurfaced(_startPosition);
        }

        float dt = Time.deltaTime;
        _airTime += dt;
        // 부푼 복어는 공기를 머금어 천천히 떨어진다.
        float gravity = _inflating ? _gravity * INFLATED_GRAVITY_SCALE : _gravity;
        _velocity += Vector3.down * (gravity * dt);
        _position += _velocity * dt;
        transform.SetPositionAndRotation(_position, BodyRotation());

        if (_inflating)
        {
            UpdateInflate(dt);
            return;
        }

        if (_velocity.y < 0f && _position.y < _waterY - 0.5f)
        {
            _spawner.HandleFishLanded(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_caught || !_jumped || other.attachedRigidbody != _spawner.PlayerBody) return;

        _caught = true;
        if (Type.InflateScale > 0f)
        {
            _inflating = true;
            _spawner.HandlePufferTouched(this);
        }
        else
        {
            _spawner.HandleFishCaught(this);
        }
    }

    /// <summary>
    /// 물고기 몸과 판정 범위를 만들고, delay 뒤 start에서 launchVelocity로 튀어 오르게 준비한다.
    /// 각 인자를 사용하며, 자식 몸 오브젝트, 콜라이더, 이동 상태를 만든다.
    /// </summary>
    public void Launch(FishSpawnerV2 spawner, FishTypeV2 type, Mesh mesh, Material material, MaterialPropertyBlock colorBlock,
        Vector3 start, Vector3 launchVelocity, float delay, float waterY, float hitRadius)
    {
        _spawner = spawner;
        Type = type;
        _startPosition = start;
        _position = start;
        _velocity = launchVelocity;
        _delay = delay;
        _waterY = waterY;
        _gravity = -Physics.gravity.y;
        transform.position = start;

        // 물고기 메시는 옆모습이 xz에 누운 형태라 z축으로 90도 세운다. 게는 위에서 본 모양이라 그대로 둔다.
        GameObject body = new GameObject("Body");
        body.transform.SetParent(transform, false);
        if (type.Shape == FishShapeV2.Fish)
        {
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        body.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = body.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.SetPropertyBlock(colorBlock);
        meshRenderer.enabled = false;
        _body = body.transform;
        _bodyRenderer = meshRenderer;

        Rigidbody fishBody = gameObject.AddComponent<Rigidbody>();
        fishBody.isKinematic = true;
        SphereCollider hitArea = gameObject.AddComponent<SphereCollider>();
        hitArea.isTrigger = true;
        hitArea.radius = hitRadius;
    }

    /// <summary>
    /// 날아가는 동안의 몸 방향을 구한다. 물고기는 머리가 진행 방향을 보며 몸을 흔들고, 게는 납작하게 빙글빙글 돈다.
    /// _velocity, _airTime, 종류 모양을 사용하며, 회전을 반환한다.
    /// </summary>
    private Quaternion BodyRotation()
    {
        if (Type.Shape == FishShapeV2.Crab)
        {
            return Quaternion.Euler(Mathf.Sin(_airTime * 6f) * 20f, _airTime * CRAB_SPIN_SPEED, 0f);
        }
        if (Type.Shape == FishShapeV2.Tire)
        {
            // 굴러가듯 세워서 돌면서 좌우로 털썩털썩 흔들린다.
            return Quaternion.Euler(_airTime * TIRE_ROLL_SPEED, Mathf.Sin(_airTime * 9f) * 25f, 90f + Mathf.Sin(_airTime * 13f) * 30f);
        }
        if (Type.Shape == FishShapeV2.Can)
        {
            // 앞뒤로 뒤집히면서 불규칙하게 파닥인다.
            return Quaternion.Euler(_airTime * CAN_FLIP_SPEED, Mathf.Sin(_airTime * 11f) * 40f, Mathf.Sin(_airTime * 17f) * 35f);
        }

        Quaternion wiggle = Quaternion.Euler(0f, Mathf.Sin(_airTime * WIGGLE_SPEED) * WIGGLE_ANGLE, 0f);
        return Quaternion.LookRotation(_velocity) * wiggle;
    }

    /// <summary>
    /// 복어 몸을 공처럼 부풀리고, 다 부풀면 잡힌 것으로 스포너에 알린다.
    /// dt와 Type.InflateScale을 사용하며, 몸 크기와 _inflateTimer를 변경한다.
    /// </summary>
    private void UpdateInflate(float dt)
    {
        _inflateTimer += dt;
        float t = Mathf.Clamp01(_inflateTimer / INFLATE_TIME);
        // 처음에 확 부풀고 끝에서 살짝 넘쳤다가 돌아오는 느낌을 준다.
        float grow = 1f + (Type.InflateScale - 1f) * (Mathf.Sin(t * Mathf.PI * 0.5f) + Mathf.Sin(t * Mathf.PI) * 0.15f);
        // 세운 몸 기준 로컬 x는 높이, y는 두께, z는 길이다.
        _body.localScale = new Vector3(grow, grow * 1.3f, 1f + (grow - 1f) * 0.3f);

        if (t >= 1f)
        {
            _spawner.HandleFishCaught(this);
        }
    }
}
