using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BlackHoleLensing : MonoBehaviour
{
    private static int _activeInstances;
    private static readonly (int Source, int Target)[] _diskFloatProperties = Array.ConvertAll(
        new[] { "_Intensity", "_Opacity", "_InnerRadius", "_OuterRadius", "_EdgeSoftness", "_RotationSpeed", "_NoiseScale", "_SpiralStrength" },
        property => (Shader.PropertyToID(property), Shader.PropertyToID("_Disk" + property.Substring(1))));

    [Header("렌더링 참조")]
    [SerializeField] private Renderer _eventHorizon;
    [SerializeField] private Renderer _accretionDisk;

    [Header("런타임 상태")]
    private MaterialPropertyBlock _properties;

    public static bool IsActive => _activeInstances > 0;

    void OnEnable()
    {
        _activeInstances++;
        _properties = new MaterialPropertyBlock();
        RenderPipelineManager.beginContextRendering += BeginContextRendering;
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
    }

    void OnDisable()
    {
        _activeInstances--;
        RenderPipelineManager.beginContextRendering -= BeginContextRendering;
        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        if (_eventHorizon == null)
            return;
        _eventHorizon.GetPropertyBlock(_properties);
        _properties.SetFloat("_DiskEnabled", 0f);
        _eventHorizon.SetPropertyBlock(_properties);
    }

    /// <summary>
    /// 렌더 컨텍스트의 카메라 목록을 사용해 URP 카메라 설정 생성 전에 깊이와 불투명 색상 텍스처를 요청한다.
    /// 각 카메라의 requiresDepthTexture 및 requiresColorTexture를 활성화한다.
    /// </summary>
    private void BeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        foreach (Camera camera in cameras)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.requiresDepthTexture = true;
            cameraData.requiresColorTexture = true;
        }
    }

    /// <summary>
    /// 렌더링할 카메라 이벤트를 받아 형제 원반의 현재 Transform과 외형을 EventHorizon에 전달한다.
    /// Inspector 참조가 지정되지 않은 편집 중 상태에서는 머티리얼 상태를 변경하지 않는다.
    /// </summary>
    private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (_eventHorizon == null || _accretionDisk == null)
            return;

        Material material = _accretionDisk.sharedMaterial;
        _eventHorizon.GetPropertyBlock(_properties);
        _properties.SetColor("_LensBackgroundColor", camera.backgroundColor);
        _properties.SetFloat("_DiskEnabled", _accretionDisk.enabled && _accretionDisk.gameObject.activeInHierarchy ? 1f : 0f);
        _properties.SetMatrix("_DiskWorldToLocal", _accretionDisk.transform.worldToLocalMatrix);
        _properties.SetColor("_DiskInnerColor", material.GetColor("_InnerColor"));
        _properties.SetColor("_DiskOuterColor", material.GetColor("_OuterColor"));
        foreach (var property in _diskFloatProperties)
            _properties.SetFloat(property.Target, material.GetFloat(property.Source));
        _eventHorizon.SetPropertyBlock(_properties);
    }
}
