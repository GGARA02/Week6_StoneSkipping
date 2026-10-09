using System;

using UnityEngine;

// 빈 양동이의 특수 동작. 던진 뒤 SPACE 판정에 성공할 때마다 안쪽 물이 한 칸씩 차오르고, 가득 차면 OnFilled를 보낸다.
// 미끄러지는 중의 입력은 세지 않으며, MISS가 나도 차오른 물은 그대로 둔다.
public class BucketAbility : FishAbility
{
    public static event Action<Vector3> OnFilled;

    [Header("물 채우기")]
    [Tooltip("안쪽 물 높이를 조절하는 피벗. 바닥 기준이며 y 스케일 1이 가득 찬 높이다. 프리팹에서는 꺼 둔다")]
    [SerializeField]
    private Transform _waterLevel;
    [Tooltip("가득 차기까지 필요한 판정 성공 횟수")]
    [SerializeField]
    private int _fillSteps = 3;
    private int _filledSteps;

    /// <summary>
    /// 판정에 성공하면 물을 한 칸 채우고, 가득 차는 순간 양동이 위치와 함께 OnFilled를 보낸다.
    /// context의 플레이어 미끄러짐 상태와 몸 위치를 사용하며, _filledSteps와 물 높이를 변경한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        if (context.Player.IsSliding || _filledSteps >= _fillSteps) return;

        _filledSteps++;
        _waterLevel.gameObject.SetActive(true);
        _waterLevel.localScale = new Vector3(1f, (float)_filledSteps / _fillSteps, 1f);
        if (_filledSteps == _fillSteps)
        {
            OnFilled?.Invoke(context.Body.position);
        }
    }
}
