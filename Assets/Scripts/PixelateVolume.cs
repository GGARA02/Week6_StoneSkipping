using System;

using UnityEngine;
using UnityEngine.Rendering;

// 화면을 큰 픽셀 단위로 뭉개 그리는 후처리 볼륨 값. PixelateFeature가 이 값을 읽어 화면에 적용한다.
// 픽셀 크기는 볼륨 비중만큼 1(끔)과 지정한 값 사이로 섞이므로, 볼륨을 서서히 켜면 픽셀이 점점 굵어진다.
[Serializable]
[VolumeComponentMenu("Custom/Pixelate")]
public class PixelateVolume : VolumeComponent, IPostProcessComponent
{
    [Header("픽셀")]
    [Tooltip("화면 픽셀 몇 개를 한 칸으로 묶을지. 1이면 끈다")]
    [SerializeField]
    private ClampedFloatParameter _pixelSize = new ClampedFloatParameter(1f, 1f, 32f);

    public float PixelSize => _pixelSize.value;

    /// <summary>
    /// 픽셀화를 그려야 하는지 확인한다.
    /// _pixelSize를 사용하며, 한 칸이 화면 픽셀 하나보다 크면 true를 반환한다.
    /// </summary>
    public bool IsActive()
    {
        return _pixelSize.value > 1f;
    }
}
