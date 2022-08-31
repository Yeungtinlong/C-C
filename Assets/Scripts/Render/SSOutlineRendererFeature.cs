using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

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

        _afterOpaquePass.Setup(renderer);
        renderer.EnqueuePass(_afterOpaquePass);
        _afterTransparentPass.Setup(renderer);
        renderer.EnqueuePass(_afterTransparentPass);
    }

    private class AfterOpaqueRenderPass : ScriptableRenderPass
    {
        private SSOutlineSettings _ssOutlineSettings;
        private int _maskTextureID;
        private int _edgeTextureID;

        private int _outlineWidthID;
        private int _outlineColorID;
        private int _hdrIntensityID;

        private int _occlusionTextureID;
        private int _occlusionColorID;
        private int _occlusionUVScaleID;

        private int _tempID;

        private FilteringSettings _filteringSettings;
        private DrawingSettings _drawingSettings;
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

        public void Setup(ScriptableRenderer renderer)
        {
            _cameraColorTexture = renderer.cameraColorTarget;
            _ssOutlineSettings.ChangeOutlineColorChannelSO.OnChangeOutlineColor += ChangeOutlineAndOcclusionColor;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _maskTextureID = Shader.PropertyToID("_Mask");
            _edgeTextureID = Shader.PropertyToID("_Edge");

            _outlineWidthID = Shader.PropertyToID("_SampleDistance");
            _outlineColorID = Shader.PropertyToID("_OutlineColor");
            _hdrIntensityID = Shader.PropertyToID("_HDRIntensity");

            _occlusionTextureID = Shader.PropertyToID("_OcclusionTexture");
            _occlusionColorID = Shader.PropertyToID("_OcclusionColor");
            _occlusionUVScaleID = Shader.PropertyToID("_OcclusionUVScale");

            _tempID = Shader.PropertyToID("_Temp");

            _descriptor = cameraTextureDescriptor;
            _descriptor.msaaSamples = 8;
            _descriptor.colorFormat = RenderTextureFormat.ARGBHalf;

            cmd.SetGlobalFloat(_outlineWidthID, _ssOutlineSettings.OutlineWidth);
            cmd.SetGlobalColor(_outlineColorID, _ssOutlineSettings.OutlineColor);
            cmd.SetGlobalFloat(_hdrIntensityID, _ssOutlineSettings.HDRIntensity);

            cmd.SetGlobalTexture(_occlusionTextureID, _ssOutlineSettings.OcclusionTexture);
            cmd.SetGlobalColor(_occlusionColorID, _ssOutlineSettings.OcclusionColor);
            cmd.SetGlobalFloat(_occlusionUVScaleID, _ssOutlineSettings.OcclusionUVScale);

            cmd.GetTemporaryRT(_maskTextureID, _descriptor, FilterMode.Bilinear);
            ConfigureTarget(_maskTextureID);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            _drawingSettings = CreateDrawingSettings(_shaderTagIds,
                ref renderingData, SortingCriteria.CommonOpaque);
            _drawingSettings.overrideMaterial = _ssOutlineSettings.SSOutlineMaterial;

            _filteringSettings = new FilteringSettings(RenderQueueRange.all, _ssOutlineSettings.TargetLayerMask);

            GetMask(context, ref renderingData);
            MergeOcclusionIntoCamera(context, ref renderingData);
            DrawTargetLayerOpaque(context, ref renderingData);
            GetEdge(context, ref renderingData);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_maskTextureID);
            cmd.ReleaseTemporaryRT(_tempID);
            cmd.ReleaseTemporaryRT(_edgeTextureID);
        }

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

        private void GetMask(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Get Mask");
            _drawingSettings.overrideMaterialPassIndex = 0;
            context.DrawRenderers(renderingData.cullResults, ref _drawingSettings, ref _filteringSettings);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void MergeOcclusionIntoCamera(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Merge Occlusion Into Camera");

            cmd.GetTemporaryRT(_tempID, _descriptor, FilterMode.Bilinear);

            cmd.Blit(_cameraColorTexture, _tempID, _ssOutlineSettings.SSOutlineMaterial, 1);
            cmd.Blit(_tempID, _cameraColorTexture);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void DrawTargetLayerOpaque(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Draw Target Layer Opaque");
            _drawingSettings.overrideMaterial = null;
            _drawingSettings.overrideMaterialPassIndex = 0;
            context.DrawRenderers(renderingData.cullResults, ref _drawingSettings, ref _filteringSettings);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void GetEdge(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Get Edge");
            cmd.GetTemporaryRT(_edgeTextureID, _descriptor, FilterMode.Bilinear);
            cmd.Blit(_maskTextureID, _edgeTextureID, _ssOutlineSettings.SSOutlineMaterial, 2);
            cmd.SetRenderTarget(_cameraColorTexture);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private class AfterTransparentPass : ScriptableRenderPass
    {
        private SSOutlineSettings _ssOutlineSettings;
        private int _tempTextureID;

        private RenderTextureDescriptor _descriptor;
        private RenderTargetIdentifier _cameraColorTexture;

        public AfterTransparentPass(SSOutlineSettings ssOutlineSettings)
        {
            _ssOutlineSettings = ssOutlineSettings;
            renderPassEvent = _ssOutlineSettings.OutlineStage;
        }

        public void Setup(ScriptableRenderer renderer)
        {
            _cameraColorTexture = renderer.cameraColorTarget;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _tempTextureID = Shader.PropertyToID("_TempTexture");

            _descriptor = cameraTextureDescriptor;
            _descriptor.msaaSamples = 8;
            _descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            MergeOcclusionIntoCamera(context, ref renderingData);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_tempTextureID);
        }

        private void MergeOcclusionIntoCamera(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Merge Outline Into Camera");

            cmd.GetTemporaryRT(_tempTextureID, _descriptor, FilterMode.Bilinear);

            cmd.Blit(_cameraColorTexture, _tempTextureID, _ssOutlineSettings.SSOutlineMaterial, 3);
            cmd.Blit(_tempTextureID, _cameraColorTexture);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
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