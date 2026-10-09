using System;

using UnityEngine;

[DisallowMultipleComponent]
public class AidePortrait : MonoBehaviour
{
    private static readonly int _baseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int _mainTexture = Shader.PropertyToID("_MainTex");

    [Header("표정 표시")]
    [SerializeField] private Renderer _bodyRenderer;
    [SerializeField] private Texture2D[] _portraits = Array.Empty<Texture2D>();
    private MaterialPropertyBlock _properties;

    void Awake()
    {
        _properties = new MaterialPropertyBlock();
        SetExpression(0);
    }

    /// <summary>
    /// expressionName과 일치하는 Portrait 이미지로 현재 개체의 표정을 변경한다.
    /// 파일 확장자 없는 이름을 대소문자 구분 없이 비교하며 변경 성공 여부를 반환한다.
    /// </summary>
    public bool SetExpression(string expressionName)
    {
        for (int index = 0; index < _portraits.Length; index++)
        {
            Texture2D portrait = _portraits[index];
            if (portrait != null && string.Equals(portrait.name, expressionName, StringComparison.OrdinalIgnoreCase))
                return SetExpression(index);
        }

        return false;
    }

    /// <summary>
    /// portraitIndex에 해당하는 이미지를 현재 Renderer의 BaseMap으로 설정한다.
    /// 공유 머티리얼을 유지하며 개체별 텍스처만 변경하고 성공 여부를 반환한다.
    /// </summary>
    public bool SetExpression(int portraitIndex)
    {
        if (portraitIndex < 0 || portraitIndex >= _portraits.Length || _portraits[portraitIndex] == null)
            return false;

        _bodyRenderer.GetPropertyBlock(_properties);
        _properties.SetTexture(_baseMap, _portraits[portraitIndex]);
        _properties.SetTexture(_mainTexture, _portraits[portraitIndex]);
        _bodyRenderer.SetPropertyBlock(_properties);
        return true;
    }
}
