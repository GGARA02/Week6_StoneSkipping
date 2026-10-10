using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

using UnityEngine.Rendering.Universal;

// 후처리가 끝난 메인 카메라 화면을 PixelateVolume 값만큼 큰 픽셀로 뭉개 그리는 렌더러 기능.
// 볼륨이 꺼져 있거나 물고기 미리보기처럼 메인 카메라가 아니면 패스를 넣지 않는다.
public class PixelateFeature : ScriptableRendererFeature
{
    private const string SHADER_NAME = "Custom/Pixelate";
    private const string MAIN_CAMERA_TAG = "MainCamera";

    private Material _material;
    private PixelatePass _pass;

    /// <summary>
    /// 렌더러 초기화 시 픽셀화 머티리얼과 후처리 뒤에 넣을 패스를 만든다.
    /// SHADER_NAME을 사용하며, _material과 _pass를 변경한다.
    /// </summary>
    public override void Create()
    {
        _material = CoreUtils.CreateEngineMaterial(Shader.Find(SHADER_NAME));
        _pass = new PixelatePass(_material)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing,
            requiresIntermediateTexture = true,
        };
    }

    /// <summary>
    /// 메인 카메라이고 픽셀화 볼륨이 켜져 있을 때만 패스를 넣는다.
    /// renderer, renderingData와 현재 볼륨 값을 사용하며, 패스의 픽셀 크기를 변경하고 패스를 큐에 넣는다.
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.camera.CompareTag(MAIN_CAMERA_TAG)) return;
        PixelateVolume volume = VolumeManager.instance.stack.GetComponent<PixelateVolume>();
        if (!volume.IsActive()) return;

        _pass.PixelSize = volume.PixelSize;
        renderer.EnqueuePass(_pass);
    }

    /// <summary>
    /// 렌더러가 해제될 때 만든 머티리얼을 지운다.
    /// disposing은 쓰지 않으며, _material을 파괴한다.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }

    private class PixelatePass : ScriptableRenderPass
    {
        private static readonly int PIXEL_SIZE_ID = Shader.PropertyToID("_PixelSize");
        private static readonly int TARGET_SIZE_ID = Shader.PropertyToID("_TargetSize");

        private readonly Material _material;

        public float PixelSize { get; set; }

        /// <summary>
        /// 픽셀화 머티리얼을 받아 패스를 만든다.
        /// material을 사용하며, _material에 저장한다.
        /// </summary>
        public PixelatePass(Material material)
        {
            _material = material;
        }

        /// <summary>
        /// 지금 화면 색을 픽셀화 머티리얼로 새 텍스처에 옮겨 그리고, 그 텍스처를 카메라 색으로 바꾼다.
        /// renderGraph, frameData와 PixelSize를 사용하며, 머티리얼 값과 카메라 색 텍스처를 변경한다.
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalCameraData camera = frameData.Get<UniversalCameraData>();
            if (resources.isActiveTargetBackBuffer) return;

            TextureHandle source = resources.activeColorTexture;
            TextureDesc description = renderGraph.GetTextureDesc(source);
            description.name = "Pixelated Color";
            description.clearBuffer = false;
            description.depthBufferBits = DepthBits.None;
            TextureHandle destination = renderGraph.CreateTexture(description);

            _material.SetFloat(PIXEL_SIZE_ID, PixelSize);
            _material.SetVector(TARGET_SIZE_ID, new Vector4(
                camera.cameraTargetDescriptor.width, camera.cameraTargetDescriptor.height, 0f, 0f));
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, _material, 0), "Pixelate");
            resources.cameraColor = destination;
        }
    }
}
