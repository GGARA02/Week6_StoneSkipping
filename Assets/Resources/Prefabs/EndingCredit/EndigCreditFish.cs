using System;

using UnityEngine;

using TMPro;

public class EndigCreditFish : FishAbility
{
    [Header("데이터")]
    [SerializeField] private ContributorList _contributor;
    [SerializeField] private TMP_FontAsset _fontAsset;

    [Header("프리팹")]
    [Tooltip("이름 표시에 사용할 프리팹 (미할당 시 3D TextMeshPro로 자동 생성)")]
    [SerializeField] private GameObject _namePrefab;

    [Header("흩뿌리기 설정")]
    [Tooltip("3D 텍스트 크기")]
    [SerializeField] private float _fontSize;
    [Tooltip("던져진 물고기 속도 상속 비율")]
    [SerializeField] private float _inheritVelocityRate = 0.4f;
    [Tooltip("위쪽으로 솟구치는 기본 속도")]
    [SerializeField] private float _upwardSpeed = 7f;
    [Tooltip("사방으로 흩뿌려지는 무작위 속도")]
    [SerializeField] private float _scatterSpeed = 5f;
    [Tooltip("소환된 이름 유지 시간(초)")]
    private float _lifeTime = 1.5F;

    void Awake()
    {
        if (_contributor == null)
        {
            _contributor = Resources.Load<ContributorList>("Data/Contributors");
        }
        if (_fontAsset == null)
        {
            _fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSerifKR-SemiBold SDF");
        }
    }

    /// <summary>
    /// 던진 상태에서 SPACE 판정에 성공했을 때 기여자 목록에서 무작위 이름을 선택해 월드에 흩뿌린다.
    /// context와 judge를 사용하며, 소환 위치와 속도를 계산해 이름 오브젝트를 생성한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        ScatterRandomContributorName(context, judge);
    }

    /// <summary>
    /// 기여자 목록에서 무작위 이름을 하나 골라 투척체 위치에서 공중으로 흩뿌린다.
    /// context와 judge를 사용하며, 3D 텍스트 오브젝트를 생성하고 위쪽 및 사방 속도를 부여한다.
    /// </summary>
    private void ScatterRandomContributorName(ThrowContext context, SkipJudge judge)
    {
        if (_contributor == null || _contributor.contributorList == null || _contributor.contributorList.Count == 0)
        {
            return;
        }

        string contributorName = _contributor.contributorList[UnityEngine.Random.Range(0, _contributor.contributorList.Count)];
        if (string.IsNullOrWhiteSpace(contributorName))
        {
            return;
        }

        Vector3 spawnPosition = context.Player != null
            ? context.Player.transform.position + Vector3.up * 0.5f
            : transform.position + Vector3.up * 0.5f;

        Vector3 playerVelocity = context.Player != null ? context.Player.Velocity : Vector3.zero;
        Vector3 randomSpread = UnityEngine.Random.insideUnitSphere * _scatterSpeed;
        randomSpread.y = Mathf.Abs(randomSpread.y);

        Vector3 launchVelocity = playerVelocity * _inheritVelocityRate
            + Vector3.up * _upwardSpeed
            + randomSpread;

        Color textColor = judge == SkipJudge.Perfect
            ? new Color(1f, 0.85f, 0.3f, 1f)
            : Color.white;

        SpawnScatteredName(contributorName.Trim(), spawnPosition, launchVelocity, textColor);
    }

    /// <summary>
    /// 이름 문자열과 물리 파라미터를 기반으로 흩뿌려지는 이름 오브젝트를 생성한다.
    /// nameText, position, velocity, color를 사용하며, 생성된 오브젝트에 ScatteredNameEffect를 부착한다.
    /// </summary>
    private void SpawnScatteredName(string nameText, Vector3 position, Vector3 velocity, Color color)
    {
        GameObject nameObj;
        TMP_Text tmpText;

        if (_namePrefab != null)
        {
            nameObj = Instantiate(_namePrefab, position, Quaternion.identity);
            tmpText = nameObj.GetComponentInChildren<TMP_Text>();
        }
        else
        {
            nameObj = new GameObject($"Contributor_{nameText}");
            nameObj.transform.position = position;

            TextMeshPro tmp = nameObj.AddComponent<TextMeshPro>();
            tmp.text = nameText;
            tmp.fontSize = _fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            if (_fontAsset != null)
            {
                tmp.font = _fontAsset;
            }
            tmpText = tmp;
        }

        if (tmpText != null)
        {
            tmpText.text = nameText;
            tmpText.color = color;
        }

        ScatteredNameEffect effect = nameObj.AddComponent<ScatteredNameEffect>();
        effect.Initialize(velocity, tmpText, _lifeTime);
    }
}

public class ScatteredNameEffect : MonoBehaviour
{
    private TMP_Text _text;
    private Vector3 _velocity;
    private float _lifetime;
    private float _startTime;

    /// <summary>
    /// 효과의 이동 속도, 텍스트 컴포넌트, 지속 시간을 초기화한다.
    /// velocity, text, lifetime을 사용하며, 내부 상태와 시작 시각을 설정한다.
    /// </summary>
    public void Initialize(Vector3 velocity, TMP_Text text, float lifetime)
    {
        _velocity = velocity;
        _text = text;
        _lifetime = lifetime;
        _startTime = Time.time;
    }

    void Update()
    {
        transform.position += _velocity * Time.deltaTime;
        _velocity += Vector3.down * 9.81f * Time.deltaTime;
        _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, 0.5f * Time.deltaTime);

        Camera camera = Camera.main;
        if (camera != null)
        {
            transform.rotation = camera.transform.rotation;
        }

        float elapsed = Time.time - _startTime;
        if (elapsed >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float fadeStart = _lifetime * 0.6f;
        if (elapsed > fadeStart && _text != null)
        {
            float alpha = Mathf.Clamp01((_lifetime - elapsed) / (_lifetime - fadeStart));
            Color color = _text.color;
            color.a = alpha;
            _text.color = color;
        }
    }
}
