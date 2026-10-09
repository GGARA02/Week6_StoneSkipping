using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class AideNoiseScreen : MonoBehaviour
{
    [Header("노이즈 화면")]
    [SerializeField, Range(16, 128)] private int _resolution = 64;
    [SerializeField, Range(1f, 30f)] private float _framesPerSecond = 12f;
    private Texture2D _texture;
    private Color32[] _pixels;
    private System.Random _random;
    private float _elapsed;

    void Awake()
    {
        _texture = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false);
        _texture.name = "AideOfflineNoise";
        _texture.filterMode = FilterMode.Point;
        _texture.wrapMode = TextureWrapMode.Clamp;
        _pixels = new Color32[_resolution * _resolution];
        _random = new System.Random();
        GetComponent<RawImage>().texture = _texture;
    }

    void OnEnable()
    {
        _elapsed = 0f;
        RefreshNoise();
    }

    void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed < 1f / Mathf.Max(1f, _framesPerSecond)) return;
        _elapsed = 0f;
        RefreshNoise();
    }

    void OnDestroy()
    {
        if (_texture != null) Destroy(_texture);
    }

    /// <summary>
    /// 현재 해상도에 맞는 회색 잡음 픽셀을 만든다.
    /// 독립 난수 생성기를 사용해 게임 판정 난수에 영향을 주지 않고 텍스처를 갱신한다.
    /// </summary>
    private void RefreshNoise()
    {
        for (int index = 0; index < _pixels.Length; index++)
        {
            byte value = (byte)_random.Next(30, 180);
            _pixels[index] = new Color32(value, value, value, 255);
        }
        _texture.SetPixels32(_pixels);
        _texture.Apply(false);
    }
}
