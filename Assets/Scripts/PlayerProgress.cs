using UnityEngine;

public enum UpgradeType
{
    Power,
    Spin,
}

// 돈, 물고기 도감 등록 여부, 업그레이드 단계를 PlayerPrefs에 저장하고 불러온다.
public class PlayerProgress : MonoBehaviour
{
    private const string MONEY_KEY = "SkipStoneV2.Money";
    private const string FISH_KEY_PREFIX = "SkipStoneV2.Fish.";
    private const string UPGRADE_KEY_PREFIX = "SkipStoneV2.Upgrade.";
    private const string WALL_MESH_KEY = "SkipStoneV2.WallMesh";

    [Header("업그레이드")]
    [Tooltip("단계별 가격. 길이가 최대 단계 수다")]
    [SerializeField]
    private int[] _upgradeCosts = { 50, 120, 250, 450, 800 };
    [Tooltip("던지는 힘 1단계당 던지기 속도 증가율")]
    [SerializeField]
    private float _powerPerLevel = 0.08f;
    [Tooltip("스핀 1단계당 스핀 증가율")]
    [SerializeField]
    private float _spinPerLevel = 0.2f;

    public int Money { get; private set; }
    public int MaxLevel => _upgradeCosts.Length;
    public float PowerMultiplier => 1f + GetLevel(UpgradeType.Power) * _powerPerLevel;
    public float SpinMultiplier => 1f + GetLevel(UpgradeType.Spin) * _spinPerLevel;

    void Awake()
    {
        Money = PlayerPrefs.GetInt(MONEY_KEY, 0);
    }

    /// <summary>
    /// 업그레이드 현재 단계를 읽는다.
    /// type을 사용하며, 0부터 MaxLevel 사이 단계를 반환한다.
    /// </summary>
    public int GetLevel(UpgradeType type)
    {
        return PlayerPrefs.GetInt(UPGRADE_KEY_PREFIX + type, 0);
    }

    /// <summary>
    /// 다음 단계 가격을 구한다.
    /// type을 사용하며, 가격을 반환하고 이미 최대 단계면 -1을 반환한다.
    /// </summary>
    public int GetNextCost(UpgradeType type)
    {
        int level = GetLevel(type);
        return level < MaxLevel ? _upgradeCosts[level] : -1;
    }

    /// <summary>
    /// 돈이 충분하면 업그레이드를 한 단계 올리고 저장한다.
    /// type을 사용하며, 성공 여부를 반환하고 Money와 단계를 변경한다.
    /// </summary>
    public bool TryUpgrade(UpgradeType type)
    {
        int cost = GetNextCost(type);
        if (cost < 0 || Money < cost) return false;

        Money -= cost;
        PlayerPrefs.SetInt(UPGRADE_KEY_PREFIX + type, GetLevel(type) + 1);
        Save();
        return true;
    }

    /// <summary>
    /// 물고기가 도감에 등록되었는지 확인한다.
    /// fish를 사용하며, 한 번이라도 잡았으면 true를 반환한다.
    /// </summary>
    public bool IsFishRegistered(FishType fish)
    {
        return PlayerPrefs.GetInt(FISH_KEY_PREFIX + fish.Id, 0) > 0;
    }

    /// <summary>
    /// 물고기를 잡아 값만큼 돈을 받고 도감에 등록한다.
    /// fish를 사용하며, Money와 도감 등록 상태를 변경한다.
    /// </summary>
    public void AddFish(FishType fish)
    {
        Money += fish.Value;
        PlayerPrefs.SetInt(FISH_KEY_PREFIX + fish.Id, 1);
        Save();
    }

    /// <summary>
    /// mesh와 scale을 사용해 마지막으로 접촉한 Wall 조각의 모양을 영구 저장한다.
    /// 저장된 크기 보정 데이터를 새 메시로 반환하며 다음 선택과 재실행에서 같은 모양을 사용한다.
    /// </summary>
    public Mesh SaveWallMesh(Mesh mesh, Vector3 scale)
    {
        WallFishMeshData data = new WallFishMeshData(mesh, scale);
        PlayerPrefs.SetString(WALL_MESH_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        return data.ToMesh();
    }

    /// <summary>
    /// 저장된 Wall 메시 데이터를 읽어 독립된 메시로 반환한다.
    /// 입력값은 없으며 아직 접촉한 조각이 없으면 null을 반환한다.
    /// </summary>
    public Mesh LoadWallMesh()
    {
        if (!PlayerPrefs.HasKey(WALL_MESH_KEY)) return null;
        return JsonUtility.FromJson<WallFishMeshData>(PlayerPrefs.GetString(WALL_MESH_KEY)).ToMesh();
    }

    /// <summary>
    /// 돈과 변경된 값을 디스크에 저장한다.
    /// Money를 사용하며, PlayerPrefs를 저장한다.
    /// </summary>
    private void Save()
    {
        PlayerPrefs.SetInt(MONEY_KEY, Money);
        PlayerPrefs.Save();
    }
}
