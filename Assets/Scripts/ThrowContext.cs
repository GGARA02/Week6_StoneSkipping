using UnityEngine;

// 던진 물고기의 특수 동작에 넘기는 대상 묶음. 넘길 대상이 늘어도 FishAbility 메서드 시그니처는 그대로 둔다.
public readonly struct ThrowContext
{
    public PlayerController Player { get; }
    public FishMeshGenerator Shape { get; }
    public Transform Body { get; }

    /// <summary>
    /// 대상 묶음을 만든다.
    /// 각 대상을 그대로 저장한다.
    /// </summary>
    public ThrowContext(PlayerController player, FishMeshGenerator shape, Transform body)
    {
        Player = player;
        Shape = shape;
        Body = body;
    }
}
