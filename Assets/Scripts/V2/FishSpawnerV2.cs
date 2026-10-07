using System;
using System.Collections.Generic;

using UnityEngine;

// 돌이 날아가는 동안, 돌이 곧 지나갈 자리 근처 수면에서 물고기가 튀어 오르게 한다.
// 돌로 맞추면 돈을 받고 물고기를 보관한다.
public class FishSpawnerV2 : MonoBehaviour
{
    private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

    [Header("참조")]
    [SerializeField]
    private PlayerControllerV2 _player;
    [SerializeField]
    private PlayerProgressV2 _progress;
    [SerializeField]
    private SkipEffectV2 _effect;
    [SerializeField]
    private GameObject _water;
    [SerializeField]
    private Material _fishMaterial;
    private Rigidbody _playerBody;
    private float _waterY;

    [Header("물고기 종류")]
    // 크기를 2배로 키우면 면적이 4배가 되므로 양력과 마찰 배율은 1/4로 줄여 같은 손맛을 유지한다.
    [SerializeField]
    private FishTypeV2[] _fishTypes =
    {
        new FishTypeV2("mackerel", "MACKEREL", 10, new Color(0.32f, 0.5f, 0.62f), 0.6f,
            3.2f, 0.9f, 0.6f, 0.9f, 1.15f, 0.7f, 0.75f, 0.25f, 1f),
        new FishTypeV2("flyingfish", "FLYING FISH", 30, new Color(0.45f, 0.72f, 0.98f), 0.3f,
            2.8f, 0.8f, 0.56f, 0.8f, 1f, 0.9f, 0.75f, 0.25f, 0.5f),
        new FishTypeV2("flounder", "FLOUNDER", 80, new Color(0.8f, 0.63f, 0.38f), 0.1f,
            3f, 2f, 0.3f, 0.6f, 0.95f, 1.2f, 0.875f, 0.15f, 1f),
        new FishTypeV2("puffer", "PUFFER", 40, new Color(0.95f, 0.8f, 0.35f), 0.12f,
            2f, 1.3f, 1f, 0.5f, 0.9f, 1f, 0.9f, 0.3f, 1f, FishShapeV2.Fish, 0f, 2.2f),
        new FishTypeV2("crab", "CRAB", 60, new Color(0.9f, 0.32f, 0.18f), 0.1f,
            1.8f, 2.6f, 0.7f, 0.9f, 0.9f, 1.1f, 0.6f, 0.2f, 1f, FishShapeV2.Crab),
        new FishTypeV2("tire", "TIRE", 5, new Color(0.13f, 0.13f, 0.14f), 0.12f,
            2.4f, 2.4f, 0.8f, 0f, 0.85f, 1.2f, 0.8f, 0.3f, 1f, FishShapeV2.Tire),
        new FishTypeV2("can", "CRUSHED CAN", 3, new Color(0.82f, 0.16f, 0.15f), 0.15f,
            1.8f, 1f, 0.45f, 0f, 1.1f, 0.8f, 1.6f, 0.5f, 1f, FishShapeV2.Can),
        new FishTypeV2("babyshark", "BABY SHARK", 300, new Color(0.5f, 0.58f, 0.68f), 0.025f,
            5f, 1.3f, 1f, 1.4f, 0.9f, 0.7f, 0.35f, 0.12f, 1f, FishShapeV2.Fish, 0.8f),
    };
    private readonly Dictionary<FishTypeV2, Mesh> _meshes = new Dictionary<FishTypeV2, Mesh>();
    private readonly Dictionary<FishTypeV2, MaterialPropertyBlock> _colorBlocks = new Dictionary<FishTypeV2, MaterialPropertyBlock>();

    [Header("출현")]
    [Tooltip("물고기가 튀어 오르는 간격 범위(초)")]
    [SerializeField]
    private Vector2 _spawnIntervalRange = new Vector2(0.35f, 0.9f);
    [Tooltip("한 번에 튀어 오르는 마릿수 범위")]
    [SerializeField]
    private Vector2Int _schoolSizeRange = new Vector2Int(1, 3);
    [Tooltip("몇 초 뒤 돌이 지나갈 자리에서 물고기가 최고점에 오를지")]
    [SerializeField]
    private Vector2 _leadTimeRange = new Vector2(1f, 2f);
    [Tooltip("돌 진행 경로에서 옆으로 벗어나는 최대 거리. A/D로 맞추러 갈 수 있는 정도")]
    [SerializeField]
    private float _maxSideOffset = 9f;
    [SerializeField]
    private Vector2 _jumpHeightRange = new Vector2(3f, 8f);
    [Tooltip("물고기가 돌 경로를 가로지르는 속도 범위")]
    [SerializeField]
    private Vector2 _crossSpeedRange = new Vector2(2f, 5f);
    [SerializeField]
    private int _maxFish = 16;
    [Tooltip("돌이 이보다 느리면 물고기가 나오지 않는다")]
    [SerializeField]
    private float _minStoneSpeed = 10f;
    [Tooltip("물고기 크기에 더하는 판정 여유 반지름")]
    [SerializeField]
    private float _hitRadiusBonus = 0.6f;
    [SerializeField]
    private Color _catchFlashColor = new Color(1f, 0.85f, 0.3f, 0.3f);
    [Tooltip("복어가 부풀 때 돌을 위로 튕겨 올리는 속도")]
    [SerializeField]
    private float _pufferBounce = 10f;
    private readonly List<FishV2> _activeFish = new List<FishV2>();
    private float _nextSpawnTime;
    public event Action<FishTypeV2> OnFishCaught;

    public Rigidbody PlayerBody => _playerBody;
    public IReadOnlyList<FishTypeV2> FishTypes => _fishTypes;

    void Awake()
    {
        _playerBody = _player.GetComponent<Rigidbody>();
        _waterY = _water.GetComponent<Collider>().bounds.max.y;
        foreach (FishTypeV2 fish in _fishTypes)
        {
            _meshes[fish] = StoneMeshGenerator.BuildFishMesh(fish);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(BASE_COLOR_ID, fish.Color);
            _colorBlocks[fish] = block;
        }
    }

    void OnDestroy()
    {
        foreach (Mesh mesh in _meshes.Values)
        {
            Destroy(mesh);
        }
    }

    void Update()
    {
        if (!_player.IsThrown || _player.IsGameOver || Time.time < _nextSpawnTime) return;

        _nextSpawnTime = Time.time + UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
        if (_player.Speed < _minStoneSpeed) return;

        int schoolSize = UnityEngine.Random.Range(_schoolSizeRange.x, _schoolSizeRange.y + 1);
        for (int i = 0; i < schoolSize && _activeFish.Count < _maxFish; i++)
        {
            Spawn();
        }
    }

    /// <summary>
    /// 떠 있는 물고기를 모두 없앤다. 재시작할 때 쓴다.
    /// 입력값은 없으며, _activeFish를 비운다.
    /// </summary>
    public void Clear()
    {
        foreach (FishV2 fish in _activeFish)
        {
            Destroy(fish.gameObject);
        }
        _activeFish.Clear();
    }

    /// <summary>
    /// 물고기가 수면 위로 튀어 오를 때 물보라를 만든다.
    /// point를 사용하며, 물보라와 물결을 만든다.
    /// </summary>
    public void HandleFishSurfaced(Vector3 point)
    {
        _effect.PlaySplash(point, 0.35f);
    }

    /// <summary>
    /// 물고기가 다시 물에 들어가면 물보라를 만들고 없앤다.
    /// fish를 사용하며, _activeFish에서 제거한다.
    /// </summary>
    public void HandleFishLanded(FishV2 fish)
    {
        Vector3 point = fish.transform.position;
        point.y = _waterY;
        _effect.PlaySplash(point, 0.3f);
        Remove(fish);
    }

    /// <summary>
    /// 돌에 맞은 물고기를 보관하고 돈을 주며 화면 효과와 포획 이벤트를 보낸다.
    /// fish를 사용하며, 진행 상황(돈, 물고기 수)을 변경하고 _activeFish에서 제거한다.
    /// </summary>
    public void HandleFishCaught(FishV2 fish)
    {
        _progress.AddFish(fish.Type);
        _effect.PlaySplash(fish.transform.position, 0.6f);
        _effect.PlayImpact(0.8f, _catchFlashColor);
        OnFishCaught?.Invoke(fish.Type);
        Remove(fish);
    }

    /// <summary>
    /// 복어가 돌에 닿아 부풀기 시작하면 돌을 위로 튕겨 올리고 화면 효과를 준다. 다 부풀면 HandleFishCaught가 불린다.
    /// fish를 사용하며, 돌 속도를 변경한다.
    /// </summary>
    public void HandlePufferTouched(FishV2 fish)
    {
        _player.AddBounce(_pufferBounce);
        _effect.PlayImpact(0.7f, _catchFlashColor);
    }

    /// <summary>
    /// 돌이 몇 초 뒤 지나갈 자리 근처에서 최고점에 오르도록 물고기를 튀어 오르게 한다.
    /// 돌 위치와 속도, 출현 설정을 사용하며, 새 물고기를 _activeFish에 추가한다.
    /// </summary>
    private void Spawn()
    {
        FishTypeV2 type = PickType();
        Vector3 horizontal = _player.Velocity;
        horizontal.y = 0f;
        Vector3 heading = horizontal.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, heading);

        float lead = UnityEngine.Random.Range(_leadTimeRange.x, _leadTimeRange.y);
        float sideOffset = UnityEngine.Random.Range(-_maxSideOffset, _maxSideOffset);
        Vector3 apex = _playerBody.position + horizontal * lead + side * sideOffset;
        apex.y = _waterY;

        float height = UnityEngine.Random.Range(_jumpHeightRange.x, _jumpHeightRange.y);
        float gravity = -Physics.gravity.y;
        float upSpeed = Mathf.Sqrt(2f * gravity * height);
        float timeToApex = upSpeed / gravity;

        // 돌 경로 쪽으로 가로질러 헤엄치게 해서, 옆으로 벗어난 물고기도 경로를 지나가게 한다.
        float crossSign = sideOffset > 0f ? -1f : 1f;
        Vector3 cross = side * (crossSign * UnityEngine.Random.Range(_crossSpeedRange.x, _crossSpeedRange.y));
        Vector3 start = apex - cross * timeToApex;
        float delay = Mathf.Max(0f, lead - timeToApex);
        float hitRadius = Mathf.Max(type.Length * 0.5f + type.TailLength, type.Height * 0.5f) + _hitRadiusBonus;

        FishV2 fish = new GameObject("Fish_" + type.Id).AddComponent<FishV2>();
        fish.Launch(this, type, _meshes[type], _fishMaterial, _colorBlocks[type],
            start, cross + Vector3.up * upSpeed, delay, _waterY, hitRadius);
        _activeFish.Add(fish);
    }

    /// <summary>
    /// 출현 가중치에 따라 물고기 종류를 고른다.
    /// _fishTypes의 SpawnWeight를 사용하며, 고른 종류를 반환한다.
    /// </summary>
    private FishTypeV2 PickType()
    {
        float total = 0f;
        foreach (FishTypeV2 fish in _fishTypes)
        {
            total += fish.SpawnWeight;
        }

        float pick = UnityEngine.Random.Range(0f, total);
        foreach (FishTypeV2 fish in _fishTypes)
        {
            pick -= fish.SpawnWeight;
            if (pick <= 0f) return fish;
        }
        return _fishTypes[_fishTypes.Length - 1];
    }

    /// <summary>
    /// 물고기를 목록에서 빼고 오브젝트를 없앤다.
    /// fish를 사용하며, _activeFish를 변경한다.
    /// </summary>
    private void Remove(FishV2 fish)
    {
        _activeFish.Remove(fish);
        Destroy(fish.gameObject);
    }
}
