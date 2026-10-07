using UnityEngine;

public enum UpgradeTypeV2
{
    Power,
    Spin,
}

// 돈, 잡은 물고기 수, 업그레이드 단계를 PlayerPrefs에 저장하고 불러온다.
public class PlayerProgressV2 : MonoBehaviour
{
    private const string MONEY_KEY = "SkipStoneV2.Money";
    private const string FISH_KEY_PREFIX = "SkipStoneV2.Fish.";
    private const string UPGRADE_KEY_PREFIX = "SkipStoneV2.Upgrade.";

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
    public float PowerMultiplier => 1f + GetLevel(UpgradeTypeV2.Power) * _powerPerLevel;
    public float SpinMultiplier => 1f + GetLevel(UpgradeTypeV2.Spin) * _spinPerLevel;

    void Awake()
    {
        Money = PlayerPrefs.GetInt(MONEY_KEY, 0);
    }

    /// <summary>
    /// 업그레이드 현재 단계를 읽는다.
    /// type을 사용하며, 0부터 MaxLevel 사이 단계를 반환한다.
    /// </summary>
    public int GetLevel(UpgradeTypeV2 type)
    {
        return PlayerPrefs.GetInt(UPGRADE_KEY_PREFIX + type, 0);
    }

    /// <summary>
    /// 다음 단계 가격을 구한다.
    /// type을 사용하며, 가격을 반환하고 이미 최대 단계면 -1을 반환한다.
    /// </summary>
    public int GetNextCost(UpgradeTypeV2 type)
    {
        int level = GetLevel(type);
        return level < MaxLevel ? _upgradeCosts[level] : -1;
    }

    /// <summary>
    /// 돈이 충분하면 업그레이드를 한 단계 올리고 저장한다.
    /// type을 사용하며, 성공 여부를 반환하고 Money와 단계를 변경한다.
    /// </summary>
    public bool TryUpgrade(UpgradeTypeV2 type)
    {
        int cost = GetNextCost(type);
        if (cost < 0 || Money < cost) return false;

        Money -= cost;
        PlayerPrefs.SetInt(UPGRADE_KEY_PREFIX + type, GetLevel(type) + 1);
        Save();
        return true;
    }

    /// <summary>
    /// 잡은 물고기 수를 읽는다.
    /// fish를 사용하며, 남은 마릿수를 반환한다.
    /// </summary>
    public int GetFishCount(FishTypeV2 fish)
    {
        return PlayerPrefs.GetInt(FISH_KEY_PREFIX + fish.Id, 0);
    }

    /// <summary>
    /// 물고기를 잡아 값만큼 돈을 받고 한 마리를 보관한다.
    /// fish를 사용하며, Money와 물고기 수를 변경한다.
    /// </summary>
    public void AddFish(FishTypeV2 fish)
    {
        Money += fish.Value;
        PlayerPrefs.SetInt(FISH_KEY_PREFIX + fish.Id, GetFishCount(fish) + 1);
        Save();
    }

    /// <summary>
    /// 보관 중인 물고기 한 마리를 던지기용으로 꺼낸다.
    /// fish를 사용하며, 성공 여부를 반환하고 물고기 수를 변경한다.
    /// </summary>
    public bool TryUseFish(FishTypeV2 fish)
    {
        int count = GetFishCount(fish);
        if (count <= 0) return false;

        PlayerPrefs.SetInt(FISH_KEY_PREFIX + fish.Id, count - 1);
        Save();
        return true;
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
