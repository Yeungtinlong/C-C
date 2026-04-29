using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

class SSOutlineContextItem : ContextItem
{
    public TextureHandle edgeTextureHandle;

    public override void Reset() { }
}

public class SSOutlineRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private SSOutlineSettings _ssOutlineSettings = new SSOutlineSettings();
    private AfterOpaqueRenderPass _afterOpaquePass;
    private AfterTransparentPass _afterTransparentPass;

    public override void Create()
    {
        _afterOpaquePass = new AfterOpaqueRenderPass(_ssOutlineSettings);
        _afterTransparentPass = new AfterTransparentPass(_ssOutlineSettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_ssOutlineSettings.SSOutlineMaterial == null)
        {
            return;
        }

        _afterOpaquePass.Setup(renderer, renderingData.cameraData.cameraTargetDescriptor);
        renderer.EnqueuePass(_afterOpaquePass);
        _afterTransparentPass.Setup(renderer, renderingData.cameraData.cameraTargetDescriptor);
        renderer.EnqueuePass(_afterTransparentPass);
    }

    protected override void Dispose(bool disposing)
    {
        _afterTransparentPass.Cleanup();
        _afterTransparentPass = null;

        _afterOpaquePass.Cleanup();
        _afterOpaquePass = null;

        base.Dispose(disposing);
    }

    private class AfterOpaqueRenderPass : ScriptableRenderPass
    {
        private SSOutlineSettings _ssOutlineSettings;

        private int _maskTextureID;

        // private RTHandle _maskTextureHandle;
        private int _edgeTextureID;

        private int _outlineWidthID;
        private int _outlineColorID;
        private int _hdrIntensityID;

        private int _occlusionTextureID;
        private int _occlusionColorID;
        private int _occlusionUVScaleID;

        private int _tempID;

        // private RTHandle _tempTextureHandle;
        // private RTHandle _edgeTextureHandle;
        private RTHandle _occlusionTextureHandle;

        // private FilteringSettings _filteringSettings;
        // private DrawingSettings _drawingSettings;
        private RenderTextureDescriptor _descriptor;

        private RenderTargetIdentifier _cameraColorTexture;
        private List<ShaderTagId> _shaderTagIds = new List<ShaderTagId>();

        public AfterOpaqueRenderPass(SSOutlineSettings ssOutlineSettings)
        {
            _ssOutlineSettings = ssOutlineSettings;
            renderPassEvent = _ssOutlineSettings.OcclusionStage;
            for (int i = 0; i < _ssOutlineSettings.ShaderTagIds.Count; i++)
            {
                _shaderTagIds.Add(new ShaderTagId(_ssOutlineSettings.ShaderTagIds[i]));
            }
        }

        public void Setup(ScriptableRenderer renderer, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // _cameraColorTexture = renderer.cameraColorTarget;
            _ssOutlineSettings.ChangeOutlineColorChannelSO.OnChangeOutlineColor += ChangeOutlineAndOcclusionColor;

            // _maskTextureID = Shader.PropertyToID("_Mask");
            // _edgeTextureID = Shader.PropertyToID("_Edge");

            // _outlineWidthID = Shader.PropertyToID("_SampleDistance");
            // _outlineColorID = Shader.PropertyToID("_OutlineColor");
            // _hdrIntensityID = Shader.PropertyToID("_HDRIntensity");

            // _occlusionTextureID = Shader.PropertyToID("_OcclusionTexture");
            // _occlusionColorID = Shader.PropertyToID("_OcclusionColor");
            // _occlusionUVScaleID = Shader.PropertyToID("_OcclusionUVScale");

            // _tempID = Shader.PropertyToID("_Temp");

            _descriptor = cameraTextureDescriptor;
            _descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
            _descriptor.depthBufferBits = 0;
            // _descriptor.msaaSamples = 8;

            // RenderingUtils.ReAllocateHandleIfNeeded(ref _maskTextureHandle, descriptor, name: "_Mask");
            // RenderingUtils.ReAllocateHandleIfNeeded(ref _tempTextureHandle, _descriptor, name: "_Temp");
            // RenderingUtils.ReAllocateHandleIfNeeded(ref _edgeTextureHandle, _descriptor, name: "_Edge");

            if (_occlusionTextureHandle == null)
            {
                _occlusionTextureHandle = RTHandles.Alloc(_ssOutlineSettings.OcclusionTexture);
            }
        }

        public void Cleanup()
        {
            // RTHandles.Release(_maskTextureHandle);
            // RTHandles.Release(_tempTextureHandle);
            // RTHandles.Release(_edgeTextureHandle);
            RTHandles.Release(_occlusionTextureHandle);
            // _maskTextureHandle = null;
            // _tempTextureHandle = null;
            // _edgeTextureHandle = null;
            _occlusionTextureHandle = null;
        }

        class GetMaskPassData
        {
            public RendererListHandle rendererListHandle;
        }

        class MergeOcclusionIntoCameraPassData
        {
            public Material ssOutlineMaterial;
            public int shaderPassIndex;
            public float occlusionUVScale;
            public Color occlusionColor;
            public TextureHandle occlusionTexture;
            public TextureHandle mask;
            public TextureHandle src;
        }

        class DrawTargetLayerOpaquePassData
        {
            public RendererListHandle rendererListHandle;
        }

        class GetEdgePassData
        {
            public TextureHandle mask;
            public Material ssOutlineMaterial;
            public int shaderPassIndex;
            public float sampleDistance;
            public Color outlineColor;
            public float hdrIntensity;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();
            TextureHandle activeColorTexture = resourceData.activeColorTexture;
            TextureHandle activeDepthTexture = resourceData.activeDepthTexture;
            var ssOutlineContextItem = frameData.GetOrCreate<SSOutlineContextItem>();
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIds, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, _ssOutlineSettings.TargetLayerMask);

            var activeColorDesc = renderGraph.GetTextureDesc(activeColorTexture);

            var maskDesc = activeColorDesc;
            maskDesc.name = "_MaskTex";
            maskDesc.msaaSamples = MSAASamples.None;
            maskDesc.depthBufferBits = DepthBits.None;
            TextureHandle maskTextureHandle = renderGraph.CreateTexture(maskDesc);

            var tempDesc = activeColorDesc;
            tempDesc.name = "_TempTex";
            tempDesc.depthBufferBits = DepthBits.None;
            TextureHandle tempTextureHandle = renderGraph.CreateTexture(tempDesc);
            TextureHandle occlusionTextureHandle = renderGraph.ImportTexture(_occlusionTextureHandle);

            var edgeDesc = new TextureDesc(activeColorDesc.width, activeColorDesc.height);
            edgeDesc.colorFormat = activeColorDesc.colorFormat;
            edgeDesc.name = "_EdgeTex";
            edgeDesc.depthBufferBits = DepthBits.None;
            edgeDesc.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;

            ssOutlineContextItem.edgeTextureHandle = renderGraph.CreateTexture(edgeDesc);

            // 画一个白影在特定Mesh上
            using (var builder = renderGraph.AddRasterRenderPass("GetMask", out GetMaskPassData passData))
            {
                DrawingSettings getMaskDrawingSettings = drawingSettings;
                getMaskDrawingSettings.overrideMaterial = _ssOutlineSettings.SSOutlineMaterial;
                getMaskDrawingSettings.overrideMaterialPassIndex = 0;
                getMaskDrawingSettings.overrideShaderPassIndex = 0;
                RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, getMaskDrawingSettings, filteringSettings);
                RendererListHandle rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

                passData.rendererListHandle = rendererListHandle;
                builder.SetRenderAttachment(maskTextureHandle, 0);
                builder.UseRendererList(passData.rendererListHandle);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (GetMaskPassData passData, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, Color.black);
                    ctx.cmd.DrawRendererList(passData.rendererListHandle);
                });
            }

            // activeColorTexture -> temp
            using (var builder = renderGraph.AddRasterRenderPass("MergeOcclusionIntoCamera", out MergeOcclusionIntoCameraPassData passData))
            {
                passData.ssOutlineMaterial = _ssOutlineSettings.SSOutlineMaterial;
                passData.shaderPassIndex = 1;
                passData.occlusionUVScale = _ssOutlineSettings.OcclusionUVScale;
                passData.occlusionColor = _ssOutlineSettings.OcclusionColor;
                passData.occlusionTexture = occlusionTextureHandle;
                passData.mask = maskTextureHandle;
                passData.src = activeColorTexture;

                builder.UseTexture(passData.occlusionTexture, AccessFlags.Read);
                builder.UseTexture(passData.mask, AccessFlags.Read);
                builder.UseTexture(passData.src, AccessFlags.Read);
                builder.SetRenderAttachment(tempTextureHandle, 0);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (MergeOcclusionIntoCameraPassData passData, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalFloat("_OcclusionUVScale", passData.occlusionUVScale);
                    ctx.cmd.SetGlobalColor("_OcclusionColor", passData.occlusionColor);
                    ctx.cmd.SetGlobalTexture("_OcclusionTexture", passData.occlusionTexture);
                    ctx.cmd.SetGlobalTexture("_Mask", passData.mask);
                    ctx.cmd.SetGlobalTexture("_SrcTex", passData.src);

                    Blitter.BlitTexture(ctx.cmd, new Vector4(1, 1, 0, 0), passData.ssOutlineMaterial, passData.shaderPassIndex);
                });
            }

            // temp -> activeColorTexture
            renderGraph.AddCopyPass(tempTextureHandle, activeColorTexture);

            using (var builder = renderGraph.AddRasterRenderPass("DrawTargetLayerOpaque", out DrawTargetLayerOpaquePassData passData))
            {
                DrawingSettings drawOpaqueDrawingSettings = drawingSettings;
                drawOpaqueDrawingSettings.overrideMaterial = null;
                drawOpaqueDrawingSettings.overrideMaterialPassIndex = 0;
                drawOpaqueDrawingSettings.overrideShaderPassIndex = 0;
                
                RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawOpaqueDrawingSettings, filteringSettings);
                RendererListHandle rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

                passData.rendererListHandle = rendererListHandle;
                builder.SetRenderAttachment(activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(activeDepthTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderFunc(static (DrawTargetLayerOpaquePassData passData, RasterGraphContext ctx) =>
                {
                    ctx.cmd.DrawRendererList(passData.rendererListHandle);
                });
            }

            // 边缘检测
            using (var builder = renderGraph.AddRasterRenderPass("GetEdge", out GetEdgePassData passData))
            {
                passData.mask = maskTextureHandle;
                passData.ssOutlineMaterial = _ssOutlineSettings.SSOutlineMaterial;
                passData.shaderPassIndex = 2;
                passData.sampleDistance = _ssOutlineSettings.OutlineWidth;
                passData.outlineColor = _ssOutlineSettings.OutlineColor;
                passData.hdrIntensity = _ssOutlineSettings.HDRIntensity;

                builder.UseTexture(passData.mask, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.SetRenderAttachment(ssOutlineContextItem.edgeTextureHandle, 0);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (GetEdgePassData passData, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture("_SrcTex", passData.mask);
                    ctx.cmd.SetGlobalFloat("_SampleDistance", passData.sampleDistance);
                    ctx.cmd.SetGlobalColor("_OutlineColor", passData.outlineColor);
                    ctx.cmd.SetGlobalFloat("_HDRIntensity", passData.hdrIntensity);
                    Blitter.BlitTexture(ctx.cmd, new Vector4(1, 1, 0, 0), passData.ssOutlineMaterial, passData.shaderPassIndex);
                });
            }
        }

        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // {
        //     _maskTextureID = Shader.PropertyToID("_Mask");
        //     _edgeTextureID = Shader.PropertyToID("_Edge");
        //
        //     _outlineWidthID = Shader.PropertyToID("_SampleDistance");
        //     _outlineColorID = Shader.PropertyToID("_OutlineColor");
        //     _hdrIntensityID = Shader.PropertyToID("_HDRIntensity");
        //
        //     _occlusionTextureID = Shader.PropertyToID("_OcclusionTexture");
        //     _occlusionColorID = Shader.PropertyToID("_OcclusionColor");
        //     _occlusionUVScaleID = Shader.PropertyToID("_OcclusionUVScale");
        //
        //     _tempID = Shader.PropertyToID("_Temp");
        //
        //     _descriptor = cameraTextureDescriptor;
        //     _descriptor.msaaSamples = 8;
        //     _descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
        //
        //     cmd.SetGlobalFloat(_outlineWidthID, _ssOutlineSettings.OutlineWidth);
        //     cmd.SetGlobalColor(_outlineColorID, _ssOutlineSettings.OutlineColor);
        //     cmd.SetGlobalFloat(_hdrIntensityID, _ssOutlineSettings.HDRIntensity);
        //
        //     cmd.SetGlobalTexture(_occlusionTextureID, _ssOutlineSettings.OcclusionTexture);
        //     cmd.SetGlobalColor(_occlusionColorID, _ssOutlineSettings.OcclusionColor);
        //     cmd.SetGlobalFloat(_occlusionUVScaleID, _ssOutlineSettings.OcclusionUVScale);
        //
        //     cmd.GetTemporaryRT(_maskTextureID, _descriptor, FilterMode.Bilinear);
        //     ConfigureTarget(_maskTextureID);
        //     ConfigureClear(ClearFlag.All, Color.black);
        // }

        // public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     _drawingSettings = CreateDrawingSettings(_shaderTagIds,
        //         ref renderingData, SortingCriteria.CommonOpaque);
        //     _drawingSettings.overrideMaterial = _ssOutlineSettings.SSOutlineMaterial;
        //
        //     _filteringSettings = new FilteringSettings(RenderQueueRange.all, _ssOutlineSettings.TargetLayerMask);
        //
        //     GetMask(context, ref renderingData);
        //     MergeOcclusionIntoCamera(context, ref renderingData);
        //     DrawTargetLayerOpaque(context, ref renderingData);
        //     GetEdge(context, ref renderingData);
        // }

        // public override void FrameCleanup(CommandBuffer cmd)
        // {
        //     cmd.ReleaseTemporaryRT(_maskTextureID);
        //     cmd.ReleaseTemporaryRT(_tempID);
        //     cmd.ReleaseTemporaryRT(_edgeTextureID);
        // }

        private void ChangeOutlineAndOcclusionColor(UnitAlignment alignment)
        {
            if (alignment == UnitAlignment.None)
            {
                _ssOutlineSettings.OcclusionColor = Color.white;
                _ssOutlineSettings.OutlineColor = Color.white;
            }
            else if (alignment == UnitAlignment.Own)
            {
                _ssOutlineSettings.OcclusionColor = _ssOutlineSettings.AllyColor;
                _ssOutlineSettings.OutlineColor = _ssOutlineSettings.AllyColor;
            }
            else if (alignment == UnitAlignment.Enemy)
            {
                _ssOutlineSettings.OcclusionColor = _ssOutlineSettings.EnemyColor;
                _ssOutlineSettings.OutlineColor = _ssOutlineSettings.EnemyColor;
            }
        }

        // private void GetMask(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     CommandBuffer cmd = CommandBufferPool.Get("Get Mask");
        //     _drawingSettings.overrideMaterialPassIndex = 0;
        //     context.DrawRenderers(renderingData.cullResults, ref _drawingSettings, ref _filteringSettings);
        //     context.ExecuteCommandBuffer(cmd);
        //     CommandBufferPool.Release(cmd);
        // }

        // private void MergeOcclusionIntoCamera(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     CommandBuffer cmd = CommandBufferPool.Get("Merge Occlusion Into Camera");
        //
        //     cmd.GetTemporaryRT(_tempID, _descriptor, FilterMode.Bilinear);
        //
        //     cmd.Blit(_cameraColorTexture, _tempID, _ssOutlineSettings.SSOutlineMaterial, 1);
        //     cmd.Blit(_tempID, _cameraColorTexture);
        //
        //     context.ExecuteCommandBuffer(cmd);
        //     CommandBufferPool.Release(cmd);
        // }

        // private void DrawTargetLayerOpaque(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     CommandBuffer cmd = CommandBufferPool.Get("Draw Target Layer Opaque");
        //     _drawingSettings.overrideMaterial = null;
        //     _drawingSettings.overrideMaterialPassIndex = 0;
        //     context.DrawRenderers(renderingData.cullResults, ref _drawingSettings, ref _filteringSettings);
        //     context.ExecuteCommandBuffer(cmd);
        //     CommandBufferPool.Release(cmd);
        // }

        // private void GetEdge(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     CommandBuffer cmd = CommandBufferPool.Get("Get Edge");
        //     cmd.GetTemporaryRT(_edgeTextureID, _descriptor, FilterMode.Bilinear);
        //     cmd.Blit(_maskTextureID, _edgeTextureID, _ssOutlineSettings.SSOutlineMaterial, 2);
        //     cmd.SetRenderTarget(_cameraColorTexture);
        //     context.ExecuteCommandBuffer(cmd);
        //     CommandBufferPool.Release(cmd);
        // }
    }

    private class AfterTransparentPass : ScriptableRenderPass
    {
        private SSOutlineSettings _ssOutlineSettings;
        private int _tempTextureID;

        private RenderTextureDescriptor _descriptor;
        // private RenderTargetIdentifier _cameraColorTexture;

        public AfterTransparentPass(SSOutlineSettings ssOutlineSettings)
        {
            _ssOutlineSettings = ssOutlineSettings;
            renderPassEvent = _ssOutlineSettings.OutlineStage;
        }

        public void Setup(ScriptableRenderer renderer, RenderTextureDescriptor descriptor)
        {
            // _cameraColorTexture = renderer.cameraColorTarget;
            _descriptor = descriptor;
            _descriptor.msaaSamples = 8;
            _descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
        }

        public void Cleanup() { }

        class MergeOcclusionIntoCameraPassData
        {
            public Material ssOutlineMaterial;
            public int shaderPassIndex;
            public TextureHandle src;
            public TextureHandle edgeTextureHandle;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var ssOutlineContextItem = frameData.Get<SSOutlineContextItem>();
            var activeColorTexture = resourceData.activeColorTexture;

            TextureDesc activeColorDesc = renderGraph.GetTextureDesc(activeColorTexture);

            var tempTextureDesc = new TextureDesc(activeColorDesc.width, activeColorDesc.height);
            tempTextureDesc.name = "_TempTex";
            tempTextureDesc.colorFormat = activeColorDesc.format;
            tempTextureDesc.depthBufferBits = DepthBits.None;
            tempTextureDesc.msaaSamples = activeColorDesc.msaaSamples;
            TextureHandle tempTextureHandle = renderGraph.CreateTexture(tempTextureDesc);

            using (var builder = renderGraph.AddRasterRenderPass("MergeOcclusionIntoCamera", out MergeOcclusionIntoCameraPassData passData))
            {
                // CommandBuffer cmd = CommandBufferPool.Get("Merge Outline Into Camera");
                // cmd.GetTemporaryRT(_tempTextureID, _descriptor, FilterMode.Bilinear);
                // cmd.Blit(_cameraColorTexture, _tempTextureID, _ssOutlineSettings.SSOutlineMaterial, 3);
                // cmd.Blit(_tempTextureID, _cameraColorTexture);
                // context.ExecuteCommandBuffer(cmd);
                // CommandBufferPool.Release(cmd);

                passData.ssOutlineMaterial = _ssOutlineSettings.SSOutlineMaterial;
                passData.shaderPassIndex = 3;
                passData.edgeTextureHandle = ssOutlineContextItem.edgeTextureHandle;
                passData.src = activeColorTexture;

                builder.UseTexture(activeColorTexture, AccessFlags.Read);
                builder.UseTexture(passData.edgeTextureHandle, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.SetRenderAttachment(tempTextureHandle, 0);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (MergeOcclusionIntoCameraPassData passData, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture("_SrcTex", passData.src);
                    ctx.cmd.SetGlobalTexture("_Edge", passData.edgeTextureHandle);

                    Blitter.BlitTexture(ctx.cmd, new Vector4(1, 1, 0, 0), passData.ssOutlineMaterial, passData.shaderPassIndex);
                });
            }

            renderGraph.AddCopyPass(tempTextureHandle, activeColorTexture);
        }

        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // {
        //     _tempTextureID = Shader.PropertyToID("_TempTexture");
        //
        //     _descriptor = cameraTextureDescriptor;
        //     _descriptor.msaaSamples = 8;
        //     _descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
        // }

        // public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     MergeOcclusionIntoCamera(context, ref renderingData);
        // }

        // public override void FrameCleanup(CommandBuffer cmd)
        // {
        //     cmd.ReleaseTemporaryRT(_tempTextureID);
        // }

        // private void MergeOcclusionIntoCamera(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     CommandBuffer cmd = CommandBufferPool.Get("Merge Outline Into Camera");
        //
        //     cmd.GetTemporaryRT(_tempTextureID, _descriptor, FilterMode.Bilinear);
        //
        //     cmd.Blit(_cameraColorTexture, _tempTextureID, _ssOutlineSettings.SSOutlineMaterial, 3);
        //     cmd.Blit(_tempTextureID, _cameraColorTexture);
        //
        //     context.ExecuteCommandBuffer(cmd);
        //     CommandBufferPool.Release(cmd);
        // }
    }

    [Serializable]
    private class SSOutlineSettings
    {
        public LayerMask TargetLayerMask;
        public List<string> ShaderTagIds = new List<string>();
        public Material SSOutlineMaterial;

        [Header("Listening on")] public ChangeOutlineColorChannelSO ChangeOutlineColorChannelSO;

        [Header("Setting For Occlusion")] public Color OcclusionColor = Color.white;
        public Texture2D OcclusionTexture;
        public float OcclusionUVScale = 1f;
        public OcclusionTexturePassType OcclusionPass;

        [Header("Setting For Outline")] public Color OutlineColor = Color.black;
        public float OutlineWidth = 0.1f;
        [Range(0f, 5f)] public float HDRIntensity = 0;

        [Header("Ally And Enemy Color")] public Color AllyColor = Color.green;
        public Color EnemyColor = Color.red;

        [Header("Advanced Setting")] public RenderPassEvent OcclusionStage = RenderPassEvent.AfterRenderingOpaques;
        public RenderPassEvent OutlineStage = RenderPassEvent.AfterRenderingTransparents;
    }

    private enum OcclusionTexturePassType
    {
        AlphaOnly,
        RGB, // not implement
        RGBA // not implement
    }
}