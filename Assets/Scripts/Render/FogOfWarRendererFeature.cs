using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class FogOfWarRendererFeature : ScriptableRendererFeature
{
    class LerpPassData
    {
        public TextureHandle fowTexture;
        public TextureHandle lastTexture;
        public TextureHandle dest;
        public Material blitMaterial;
        public int shaderPass;
    }

    class BlurPassData
    {
        public TextureHandle src;
        public TextureHandle dest;
        public Material blitMaterial;
        public int shaderPass;
        public float blurRadius;
        public float width;
        public float height;
    }

    class FowPassData
    {
        public TextureHandle blurTexture;
        public TextureHandle dest;
        public RendererListHandle rendererListHandle;
        public float fowDarkness;
    }

    [SerializeField] private FogOfWarSetting _setting = new FogOfWarSetting();
    private FogOfWarRenderPass _pass;

    public override void Create()
    {
        _pass = new FogOfWarRenderPass(_setting);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _pass.Setup(renderer, renderingData.cameraData.cameraTargetDescriptor);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass.Cleanup();
        _pass = null;
        base.Dispose(disposing);
    }

    public class FogOfWarRenderPass : ScriptableRenderPass
    {
        private readonly ShaderTagId _fogOfWarTag = new ShaderTagId("FogOfWarPlane");

        private FogOfWarSetting _setting;
        private int _fowDarknessID;
        private int _blurRadiusID;

        private RenderTextureDescriptor _descriptor;
        private RTHandle _lerpTexHandle;
        private RTHandle _blurTexHandle;

        private RTHandle _fowTex;
        private RTHandle _lastFowTex;

        // private RenderTargetIdentifier _cameraColorTarget;

        public FogOfWarRenderPass(FogOfWarSetting setting)
        {
            _setting = setting;
            renderPassEvent = _setting.RenderPassEvent;
        }

        public void Setup(ScriptableRenderer renderer, RenderTextureDescriptor cameraTextureDescriptor)
        {
            Texture2D fowTex = _setting.VisibilitySystemSO.FogOfWarTexture2D;
            if (fowTex == null)
                return;

            Texture2D lastFowTex = _setting.VisibilitySystemSO.LastFogOfWarTexture2D;
            if (lastFowTex == null)
                return;

            if (_fowTex == null || _lastFowTex == null)
            {
                _fowTex = RTHandles.Alloc(fowTex);
                _lastFowTex = RTHandles.Alloc(lastFowTex);
            }

            // _cameraColorTarget = renderer.cameraColorTarget;
            _descriptor = cameraTextureDescriptor;
            _descriptor.colorFormat = RenderTextureFormat.R8;
            _descriptor.depthBufferBits = 0;
            _descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateHandleIfNeeded(ref _lerpTexHandle, _descriptor, name: "_LerpTexture");
            RenderingUtils.ReAllocateHandleIfNeeded(ref _blurTexHandle, _descriptor, name: "_BlurTexture");
        }

        public void Cleanup()
        {
            Debug.Log("FOW Cleanup");
            RTHandles.Release(_lerpTexHandle);
            RTHandles.Release(_blurTexHandle);
            RTHandles.Release(_fowTex);
            RTHandles.Release(_lastFowTex);

            _lerpTexHandle = null;
            _blurTexHandle = null;
            _fowTex = null;
            _lastFowTex = null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            var srcFowTexture = renderGraph.ImportTexture(_fowTex);
            var blurTexture = renderGraph.ImportTexture(_blurTexHandle);
            var lastTexture = renderGraph.ImportTexture(_lastFowTex);
            var lerpTexHandle = renderGraph.ImportTexture(_lerpTexHandle);
            var activeColorTexture = resourceData.activeColorTexture;
            var activeColorTextureDescriptor = activeColorTexture.GetDescriptor(renderGraph);

            using (var builder = renderGraph.AddRasterRenderPass("Lerp last fow texture", out LerpPassData passData))
            {
                passData.fowTexture = srcFowTexture;
                passData.lastTexture = lastTexture;
                passData.dest = lerpTexHandle;
                passData.blitMaterial = _setting.FogOfWarMaterial;
                passData.shaderPass = 3;

                builder.UseTexture(passData.fowTexture, AccessFlags.Read);
                builder.UseTexture(passData.lastTexture, AccessFlags.Read);
                builder.SetRenderAttachment(passData.dest, 0);
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (LerpPassData passData, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture("_SrcTex", passData.fowTexture);
                    ctx.cmd.SetGlobalTexture("_LastTexture", passData.lastTexture);
                    Blitter.BlitTexture(ctx.cmd, new Vector4(1, 1, 0, 0), passData.blitMaterial, passData.shaderPass);
                });
            }

            // TODO: 多次Blur迭代
            using (var builder = renderGraph.AddRasterRenderPass("Blur", out BlurPassData passData))
            {
                passData.src = srcFowTexture;
                passData.dest = blurTexture;
                passData.blitMaterial = _setting.FogOfWarMaterial;
                passData.blurRadius = _setting.BlurRadius;
                passData.shaderPass = 0;
                // passData.width = activeColorTextureDescriptor.width;
                // passData.height = activeColorTextureDescriptor.height;

                builder.UseTexture(passData.src, AccessFlags.Read);
                builder.SetRenderAttachment(passData.dest, 0);
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (BlurPassData passData, RasterGraphContext ctx) =>
                {
                    // ctx.cmd.SetViewport(new Rect(0,0, passData.width, passData.height));
                    ctx.cmd.SetGlobalTexture("_SrcTex", passData.src);
                    ctx.cmd.SetGlobalFloat("_BlurRadius", passData.blurRadius);
                    Blitter.BlitTexture(ctx.cmd, new Vector4(1, 1, 0, 0), passData.blitMaterial, passData.shaderPass);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass("Fog of war plane", out FowPassData passData))
            {
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, 1 << LayerMask.NameToLayer("FOW"));
                DrawingSettings drawingSettings = CreateDrawingSettings(_fogOfWarTag, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
                drawingSettings.overrideMaterial = _setting.FogOfWarMaterial;
                drawingSettings.overrideMaterialPassIndex = 1;
                drawingSettings.overrideShaderPassIndex = 1;
                
                var rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                RendererListHandle rendererListHandle = renderGraph.CreateRendererList(in rendererListParams);

                passData.blurTexture = blurTexture;
                passData.dest = activeColorTexture;
                passData.fowDarkness = _setting.FogOfWarDarkness;
                passData.rendererListHandle = rendererListHandle;

                builder.UseTexture(passData.blurTexture, AccessFlags.Read);
                builder.SetRenderAttachment(passData.dest, 0);
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                builder.UseRendererList(rendererListHandle);

                builder.SetRenderFunc(static (FowPassData passData, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture("_BlurTexture", passData.blurTexture);
                    ctx.cmd.SetGlobalFloat("_FOWDarkness", passData.fowDarkness);
                    ctx.cmd.DrawRendererList(passData.rendererListHandle);
                });
            }
        }

        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // {
        //     _descriptor = cameraTextureDescriptor;
        //     _descriptor.colorFormat = RenderTextureFormat.R8;
        //     _descriptor.msaaSamples = 1;
        //
        //     _fowDarknessID = Shader.PropertyToID("_FOWDarkness");
        //     _blurRadiusID = Shader.PropertyToID("_BlurRadius");
        //
        //     _tempTexHandle.Init("_TempTexture");
        //     _fowTexHandle.Init("_FOWTexture");
        //     _blurTexHandle.Init("_BlurTexture");
        //     _lerpTexHandle.Init("_LerpTexture");
        //     _lastTexHandle.Init("_LastTexture");
        //
        //     cmd.SetGlobalFloat(_fowDarknessID, _setting.FogOfWarDarkness);
        //     cmd.SetGlobalFloat(_blurRadiusID, _setting.BlurRadius);
        //
        //     cmd.GetTemporaryRT(_fowTexHandle.id, _descriptor, FilterMode.Bilinear);
        //     cmd.GetTemporaryRT(_tempTexHandle.id, _descriptor, FilterMode.Bilinear);
        //     cmd.GetTemporaryRT(_blurTexHandle.id, _descriptor, FilterMode.Bilinear);
        //     cmd.GetTemporaryRT(_lerpTexHandle.id, _descriptor, FilterMode.Bilinear);
        //     cmd.GetTemporaryRT(_lastTexHandle.id, _descriptor, FilterMode.Bilinear);
        // }

        // public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     Texture2D fowTex = _setting.VisibilitySystemSO.FogOfWarTexture2D;
        //     if (fowTex == null)
        //         return;
        //
        //     Texture2D lastFowTex = _setting.VisibilitySystemSO.LastFogOfWarTexture2D;
        //     if (lastFowTex == null)
        //         return;
        //
        //     CommandBuffer cmd = CommandBufferPool.Get("Fog Of War");
        //
        //     cmd.Blit(fowTex, _fowTexHandle.id);
        //     cmd.Blit(lastFowTex, _lastTexHandle.id);
        //
        //     // Lerp with lastTexture and fowTexture
        //     cmd.Blit(_fowTexHandle.id, _tempTexHandle.id, _setting.FogOfWarMaterial, 3);
        //     Utils.Swap(ref _fowTexHandle, ref _tempTexHandle);
        //     cmd.SetGlobalTexture(_fowTexHandle.id, _fowTexHandle.id);
        //
        //     // Gauss iteration
        //     for (int i = 0; i <= _setting.GaussIteration; i++)
        //     {
        //         cmd.Blit(_fowTexHandle.id, _tempTexHandle.id, _setting.FogOfWarMaterial, 0);
        //         Utils.Swap(ref _fowTexHandle, ref _tempTexHandle);
        //     }
        //
        //     cmd.SetGlobalTexture(_blurTexHandle.id, _fowTexHandle.id);
        //
        //     cmd.SetRenderTarget(_cameraColorTarget);
        //
        //     context.ExecuteCommandBuffer(cmd);
        //     CommandBufferPool.Release(cmd);
        //
        // FilteringSettings filteringSettings =
        //     new FilteringSettings(RenderQueueRange.all, 1 << LayerMask.NameToLayer("Default"));
        //
        // DrawingSettings drawingSettings = CreateDrawingSettings(new ShaderTagId("FogOfWarPlane"), ref renderingData,
        //     SortingCriteria.CommonOpaque);
        //
        // drawingSettings.overrideMaterial = _setting.FogOfWarMaterial;
        // drawingSettings.overrideMaterialPassIndex = 1;
        //
        // context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        // }
        //
        // public override void FrameCleanup(CommandBuffer cmd)
        // {
        //     cmd.ReleaseTemporaryRT(_fowTexHandle.id);
        //     cmd.ReleaseTemporaryRT(_tempTexHandle.id);
        //     cmd.ReleaseTemporaryRT(_blurTexHandle.id);
        //     cmd.ReleaseTemporaryRT(_lerpTexHandle.id);
        //     cmd.ReleaseTemporaryRT(_lastTexHandle.id);
        // }
    }

    [Serializable]
    public class FogOfWarSetting
    {
        public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public VisibilitySystemSO VisibilitySystemSO;
        public Material FogOfWarMaterial;
        [Range(0f, 1f)] public float FogOfWarDarkness;
        [Range(0f, 5f)] public float BlurRadius;

        [Range(1, 10)] public int GaussIteration = 1;
        // [Range(0f, 5f)] public float LerpRate = 0.05f;
    }
}