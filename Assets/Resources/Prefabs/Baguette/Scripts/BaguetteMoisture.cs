using UnityEngine;
using UnityEngine.Serialization;

public class BaguetteMoisture : FishAbility
{
    [Header("머티리얼")]
    [FormerlySerializedAs("baguetteRenderer")]
    [SerializeField]
    private MeshRenderer _baguetteRenderer;
    [FormerlySerializedAs("baguetteMaterials")]
    [SerializeField]
    private Material[] _baguetteMaterials;

    [Header("속도 설정")]
    [SerializeField]
    private float _maxSpeed = 50f;
    [SerializeField]
    private float _minSpeed = 12f;

    private int _currentMaterialIndex = -1;

    void OnEnable()
    {
        _currentMaterialIndex = 0;
        if (_baguetteMaterials != null && _baguetteMaterials.Length > 0)
        {
            ChangeMaterial(0);
        }
    }

    /// <summary>
    /// 비행 중 현재 속도를 기준으로 바게트 머티리얼을 갱신한다.
    /// context와 dt를 사용하며, _currentMaterialIndex와 sharedMaterial을 변경한다.
    /// </summary>
    public override void UpdateFlight(ThrowContext context, float dt)
    {
        if (_baguetteMaterials == null || _baguetteMaterials.Length == 0)
        {
            return;
        }

        // 최대 속도에 가까울수록 건조한 머티리얼을, 정지 속도에 가까울수록 젖은 머티리얼을 선택한다.
        float t = Mathf.InverseLerp(_maxSpeed, _minSpeed, context.Player.Speed);
        int targetIndex = Mathf.Clamp(Mathf.FloorToInt(t * _baguetteMaterials.Length), 0, _baguetteMaterials.Length - 1);

        if (targetIndex != _currentMaterialIndex)
        {
            _currentMaterialIndex = targetIndex;
            ChangeMaterial(targetIndex);
        }
    }

    /// <summary>
    /// 지정된 인덱스의 머티리얼을 바게트 렌더러에 적용한다.
    /// index를 사용하며, _baguetteRenderer.sharedMaterial을 변경한다.
    /// </summary>
    private void ChangeMaterial(int index)
    {
        _baguetteRenderer.sharedMaterial = _baguetteMaterials[index];
    }
}

