using System;

using UnityEngine;

public enum AideExpression
{
    Idle,
    Angry,
    Close,
    Doya,
    Embarrassed,
    Panic,
    Shame,
    Smile1,
    Smile2,
}

[CreateAssetMenu(menuName = "Ship/Aide Dialogue Profile")]
public class AideDialogueProfile : ScriptableObject
{
    [Serializable]
    private class Dialogue
    {
        [SerializeField] private AideExpression _expression;
        [SerializeField, TextArea(2, 4)] private string _portrait;
        [SerializeField, TextArea(2, 4)] private string _projectile;

        public AideExpression Expression => _expression;
        public string Portrait => _portrait;
        public string Projectile => _projectile;

        /// <summary>
        /// 표정과 두 사용 형태의 대사를 입력받아 한 쌍의 대화 설정을 만든다.
        /// expression, portrait, projectile을 각 설정 필드에 저장한다.
        /// </summary>
        public Dialogue(AideExpression expression, string portrait, string projectile)
        {
            _expression = expression;
            _portrait = portrait;
            _projectile = projectile;
        }
    }

    [Header("표정별 대사")]
    [SerializeField] private Dialogue[] _dialogues =
    {
        new Dialogue(AideExpression.Idle, "준비됐어? 내가 지켜볼게.", "이번엔 내가 날아가는 거야?"),
        new Dialogue(AideExpression.Angry, "조금만 더 집중해 줘!", "그쪽은 벽이잖아!"),
        new Dialogue(AideExpression.Close, "침착하게, 타이밍을 보자.", "눈 감을 테니까 잘 부탁해."),
        new Dialogue(AideExpression.Doya, "봤지? 이 정도는 간단해!", "봤어? 완벽한 비행이야!"),
        new Dialogue(AideExpression.Embarrassed, "괜찮아, 다음엔 맞출 수 있어.", "앗, 방금 좀 흔들렸어!"),
        new Dialogue(AideExpression.Panic, "잠깐, 연결이 끊기고 있어…!", "으아! 조심해서 던져 줘!"),
        new Dialogue(AideExpression.Shame, "그렇게 잘하면 좀 부끄럽잖아.", "생각보다 잘 던지네…!"),
        new Dialogue(AideExpression.Smile1, "방금 그 판단, 꽤 좋았어.", "좋아, 이대로 계속 가자!"),
        new Dialogue(AideExpression.Smile2, "좋아! 정말 잘하고 있어!", "한 번 더! 더 멀리 날아가자!"),
    };

    [Header("상황별 표정 후보")]
    [SerializeField] private AideExpression[] _selectionExpressions = { AideExpression.Idle, AideExpression.Close, AideExpression.Smile1 };
    [SerializeField] private AideExpression[] _successExpressions = { AideExpression.Smile1, AideExpression.Smile2, AideExpression.Shame, AideExpression.Doya };
    [SerializeField] private AideExpression[] _failureExpressions = { AideExpression.Embarrassed, AideExpression.Angry, AideExpression.Panic };

    /// <summary>
    /// 선택 상황의 표정 후보에서 하나를 무작위로 반환한다.
    /// Inspector 후보를 사용하며 후보가 비었으면 Idle을 반환한다.
    /// </summary>
    public AideExpression PickSelection()
    {
        return Pick(_selectionExpressions, AideExpression.Idle);
    }

    /// <summary>
    /// 성공 상황의 표정 후보에서 하나를 무작위로 반환한다.
    /// Inspector 후보를 사용하며 후보가 비었으면 Smile1을 반환한다.
    /// </summary>
    public AideExpression PickSuccess()
    {
        return Pick(_successExpressions, AideExpression.Smile1);
    }

    /// <summary>
    /// 투척물 실패 상황의 표정 후보에서 하나를 무작위로 반환한다.
    /// Inspector 후보를 사용하며 후보가 비었으면 Embarrassed를 반환한다.
    /// </summary>
    public AideExpression PickFailure()
    {
        return Pick(_failureExpressions, AideExpression.Embarrassed);
    }

    /// <summary>
    /// 표정 expression에 대응하는 사용 형태별 대사를 반환한다.
    /// projectile이 true이면 투척물 대사를 선택하며 미등록 표정이면 빈 문자열을 반환한다.
    /// </summary>
    public string GetDialogue(AideExpression expression, bool projectile)
    {
        foreach (Dialogue dialogue in _dialogues)
        {
            if (dialogue.Expression == expression)
                return projectile ? dialogue.Projectile : dialogue.Portrait;
        }
        return string.Empty;
    }

    /// <summary>
    /// candidates 배열에서 표정을 하나 고른다.
    /// 배열이 비었으면 fallback을, 그렇지 않으면 무작위 표정을 반환한다.
    /// </summary>
    private static AideExpression Pick(AideExpression[] candidates, AideExpression fallback)
    {
        return candidates.Length == 0 ? fallback : candidates[UnityEngine.Random.Range(0, candidates.Length)];
    }
}
