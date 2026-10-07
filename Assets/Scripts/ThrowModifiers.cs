// 한 번 던질 때 적용하는 배율 묶음. 업그레이드와 던지는 물체(돌, 물고기)에 따라 달라진다.
public readonly struct ThrowModifiers
{
    public float SpeedMultiplier { get; }
    public float SpinMultiplier { get; }
    public float LiftMultiplier { get; }
    public float FrictionMultiplier { get; }
    public float AirGravityScale { get; }

    /// <summary>
    /// 배율 묶음을 만든다.
    /// 각 배율을 그대로 저장한다.
    /// </summary>
    public ThrowModifiers(float speedMultiplier, float spinMultiplier, float liftMultiplier, float frictionMultiplier, float airGravityScale)
    {
        SpeedMultiplier = speedMultiplier;
        SpinMultiplier = spinMultiplier;
        LiftMultiplier = liftMultiplier;
        FrictionMultiplier = frictionMultiplier;
        AirGravityScale = airGravityScale;
    }

    /// <summary>
    /// 돌을 던질 때 쓰는 배율을 만든다. 물리 특성은 그대로 두고 업그레이드 배율만 적용한다.
    /// powerMultiplier, spinMultiplier를 사용하며, ThrowModifiers를 반환한다.
    /// </summary>
    public static ThrowModifiers ForStone(float powerMultiplier, float spinMultiplier)
    {
        return new ThrowModifiers(powerMultiplier, spinMultiplier, 1f, 1f, 1f);
    }
}
