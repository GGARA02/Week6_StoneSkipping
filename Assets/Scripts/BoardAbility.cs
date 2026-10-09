using UnityEngine;

using TMPro;

// 명령서 보드의 특수 동작. 던진 뒤 처음으로 SPACE 판정에 성공하면
// 붙어 있던 명령서를 모두 내리고 같은 자리에 숨겨 둔 에러 메시지를 띄운다.
public class BoardAbility : FishAbility
{
    [Header("명령서")]
    [Tooltip("처음 판정 성공 때 끄는 명령서 오브젝트")]
    [SerializeField]
    private GameObject[] _instructions;
    [Tooltip("처음 판정 성공 때 켜는 에러 메시지 오브젝트. 프리팹에서는 꺼 둔다")]
    [SerializeField]
    private GameObject[] _errorMessages;

    [Header("상태")]
    private bool _errorShown;

    void Start()
    {
        // 던질 물고기로 만들 때 메시 필터가 빈 복사본으로 바뀌어 글자가 사라지므로 원래 글자 메시로 되돌린다.
        foreach (TextMeshPro text in GetComponentsInChildren<TextMeshPro>())
        {
            text.meshFilter.sharedMesh = text.mesh;
        }
    }

    /// <summary>
    /// 던진 보드가 처음으로 SPACE 판정에 성공하면 명령서를 끄고 에러 메시지를 켠다.
    /// _instructions, _errorMessages, _errorShown을 사용하며, context와 judge는 쓰지 않는다. 각 오브젝트 활성 상태와 _errorShown을 변경한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        if (_errorShown) return;

        _errorShown = true;
        foreach (GameObject instruction in _instructions)
        {
            instruction.SetActive(false);
        }
        foreach (GameObject errorMessage in _errorMessages)
        {
            errorMessage.SetActive(true);
        }
    }
}
