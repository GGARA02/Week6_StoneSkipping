using UnityEngine;

// 빈 양동이로 물을 다 퍼낸 자리에 보이는 파낸 물 구멍. 씬에 하나 배치해 두며, 높이는 배치한 수면 높이를 그대로 쓴다.
// 양동이가 가득 차면 꽉 찬 양동이와 물 블록을 해금하고 선택 목록의 빈 양동이 자리를 바꾼 뒤, 그 자리로 옮겨 나타난다.
// 위치는 저장해서 재시작하거나 껐다 켜도 남긴다. 구멍은 보이기만 하며 수면 판정은 그대로다.
public class WaterHole : MonoBehaviour
{
    private const string HOLE_X_KEY = "SkipStoneV2.WaterHole.X";
    private const string HOLE_Z_KEY = "SkipStoneV2.WaterHole.Z";

    [Header("참조")]
    [SerializeField]
    private FishSpawner _spawner;
    [SerializeField]
    private PlayerProgress _progress;

    [Header("물고기")]
    [Tooltip("물에서 튀어 오르는 빈 양동이. FishSpawner 물고기 목록에 있어야 한다")]
    [SerializeField]
    private Fish _emptyBucketPrefab;
    [Tooltip("가득 차면 선택 목록에서 빈 양동이 자리를 대신한다")]
    [SerializeField]
    private Fish _fullBucketPrefab;
    [Tooltip("가득 차면 함께 해금된다. FishSpawner 물고기 목록에 있어야 던질 수 있다")]
    [SerializeField]
    private Fish _waterBlockPrefab;

    void Awake()
    {
        // 숨어 있는 동안에도 나타날 수 있도록 활성 상태와 상관없이 구독을 유지한다.
        BucketAbility.OnFilled += HandleBucketFilled;
    }

    void Start()
    {
        // FishSpawner.Awake에서 선택 목록이 만들어진 뒤에 바꾼다.
        if (_progress.IsFishRegistered(_fullBucketPrefab.Type))
        {
            _spawner.ReplaceSelection(_emptyBucketPrefab, _fullBucketPrefab);
        }

        if (PlayerPrefs.HasKey(HOLE_X_KEY))
        {
            MoveTo(PlayerPrefs.GetFloat(HOLE_X_KEY), PlayerPrefs.GetFloat(HOLE_Z_KEY));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        BucketAbility.OnFilled -= HandleBucketFilled;
    }

    /// <summary>
    /// 양동이가 가득 차면 물 블록과 꽉 찬 양동이를 등록하고, 선택 목록을 바꾼 뒤 그 자리로 옮겨 나타나고 위치를 저장한다.
    /// 가득 찬 순간의 양동이 position을 사용하며, 진행 상황, 선택 목록, 구멍 위치와 활성 상태, PlayerPrefs를 변경한다.
    /// </summary>
    private void HandleBucketFilled(Vector3 position)
    {
        _progress.AddFish(_waterBlockPrefab.Type);
        _spawner.HandlePlacedFishCaught(_fullBucketPrefab.Type, new Vector3(position.x, transform.position.y, position.z));
        _spawner.ReplaceSelection(_emptyBucketPrefab, _fullBucketPrefab);

        MoveTo(position.x, position.z);
        gameObject.SetActive(true);
        PlayerPrefs.SetFloat(HOLE_X_KEY, position.x);
        PlayerPrefs.SetFloat(HOLE_Z_KEY, position.z);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 높이는 그대로 두고 수평 위치만 옮긴다.
    /// x, z를 사용하며, 구멍의 위치를 변경한다.
    /// </summary>
    private void MoveTo(float x, float z)
    {
        transform.position = new Vector3(x, transform.position.y, z);
    }
}
