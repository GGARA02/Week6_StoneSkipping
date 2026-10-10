using System.Collections.Generic;

using UnityEngine;

// 락온 카메라가 바라볼 수 있는 대상. 켜져 있는 동안만 대상 목록에 올라가 꺼지거나 사라지면 자동으로 빠진다.
public class LockOnTarget : MonoBehaviour
{
    private static readonly List<LockOnTarget> _targets = new List<LockOnTarget>();

    public static IReadOnlyList<LockOnTarget> Targets => _targets;

    void OnEnable()
    {
        _targets.Add(this);
    }

    void OnDisable()
    {
        _targets.Remove(this);
    }
}
