using System;
using System.Collections;
using System.Collections.Generic;
using CNC.Utility;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogOfWarRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private FogOfWarSetting _setting = new FogOfWarSetting();
    private FogOfWarRenderPass _pass;

    public override void Create()
    {
        _pass = new FogOfWarRenderPass(_setting);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _pass.Setup(renderer);
        renderer.EnqueuePass(_pass);
    }

    public class FogOfWarRenderPass : ScriptableRenderPass
    {
        private FogOfWarSetting _setting;
        private int _fowDarknessID;
        private int _blurRadiusID;

        private RenderTextureDescriptor _descriptor;
        private RenderTargetHandle _tempTexHandle;
        private RenderTargetHandle _fowTexHandle;
        private RenderTargetHandle _blurTexHandle;
        private RenderTargetHandle _lerpTexHandle;
        private RenderTargetHandle _lastTexHandle;

        private RenderTargetIdentifier _cameraColorTarget;

        public FogOfWarRenderPass(FogOfWarSetting setting)
        {
            _setting = setting;
            renderPassEvent = _setting.RenderPassEvent;
        }

        public void Setup(ScriptableRenderer renderer)
        {
            _cameraColorTarget = renderer.cameraColorTarget;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _descriptor = cameraTextureDescriptor;
            _descriptor.colorFormat = RenderTextureFormat.R8;
            _descriptor.msaaSamples = 1;

            _fowDarknessID = Shader.PropertyToID("_FOWDarkness");
            _blurRadiusID = Shader.PropertyToID("_BlurRadius");

            _tempTexHandle.Init("_TempTexture");
            _fowTexHandle.Init("_FOWTexture");
            _blurTexHandle.Init("_BlurTexture");
            _lerpTexHandle.Init("_LerpTexture");
            _lastTexHandle.Init("_LastTexture");

            cmd.SetGlobalFloat(_fowDarknessID, _setting.FogOfWarDarkness);
            cmd.SetGlobalFloat(_blurRadiusID, _setting.BlurRadius);
            
            cmd.GetTemporaryRT(_fowTexHandle.id, _descriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_tempTexHandle.id, _descriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_blurTexHandle.id, _descriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_lerpTexHandle.id, _descriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_lastTexHandle.id, _descriptor, FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Texture2D fowTex = _setting.VisibilitySystemSO.FogOfWarTexture2D;
            if (fowTex == null)
                return;
            
            Texture2D lastFowTex = _setting.VisibilitySystemSO.LastFogOfWarTexture2D;
            if (lastFowTex == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Fog Of War");

            cmd.Blit(fowTex, _fowTexHandle.id);
            cmd.Blit(lastFowTex, _lastTexHandle.id);
            
            // Lerp with lastTexture and fowTexture
            cmd.Blit(_fowTexHandle.id, _tempTexHandle.id, _setting.FogOfWarMaterial, 3);
            Utils.Swap(ref _fowTexHandle, ref _tempTexHandle);
            cmd.SetGlobalTexture(_fowTexHandle.id, _fowTexHandle.id);
            
            // Gauss iteration
            for (int i = 0; i <= _setting.GaussIteration; i++)
            {
                cmd.Blit(_fowTexHandle.id, _tempTexHandle.id, _setting.FogOfWarMaterial, 0);
                Utils.Swap(ref _fowTexHandle, ref _tempTexHandle);
            }
            cmd.SetGlobalTexture(_blurTexHandle.id, _fowTexHandle.id);

            cmd.SetRenderTarget(_cameraColorTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            FilteringSettings filteringSettings =
                new FilteringSettings(RenderQueueRange.all, 1 << LayerMask.NameToLayer("Default"));

            DrawingSettings drawingSettings = CreateDrawingSettings(new ShaderTagId("FogOfWarPlane"), ref renderingData,
                SortingCriteria.CommonOpaque);

            drawingSettings.overrideMaterial = _setting.FogOfWarMaterial;
            drawingSettings.overrideMaterialPassIndex = 1;

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_fowTexHandle.id);
            cmd.ReleaseTemporaryRT(_tempTexHandle.id);
            cmd.ReleaseTemporaryRT(_blurTexHandle.id);
            cmd.ReleaseTemporaryRT(_lerpTexHandle.id);
            cmd.ReleaseTemporaryRT(_lastTexHandle.id);
        }
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