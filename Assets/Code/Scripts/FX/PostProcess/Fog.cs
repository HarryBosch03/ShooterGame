using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bosch.FX.PostProcess
{
    [VolumeComponentMenuForRenderPipeline("Custom/Fog", typeof(UniversalRenderPipeline))]
    public sealed class Fog : VolumeComponent, IPostProcessComponent
    {
        public bool IsActive() => true;
        public bool IsTileCompatible() => false;

        [Header("Fog Settings")] public FloatParameter density = new(1.0f, true);
        public ColorParameter color = new(Color.gray, true);
        [Header("Fog Settings")] public ClampedFloatParameter farPlane = new(0.9f, 0.0f, 1.0f, true);
    }

    public sealed class FogPass : ScriptableRenderPass
    {
        private readonly Shader fogShader = Shader.Find("Hidden/Fog");
        private readonly Material fogMaterial;

        private RenderTargetIdentifier source;
        private RenderTargetIdentifier temp;
        
        private readonly int tempHandle = Shader.PropertyToID("_Temp");
        private static readonly int Value = Shader.PropertyToID("_Value");
        private static readonly int Color = Shader.PropertyToID("_Color");
        private static readonly int FarPlane = Shader.PropertyToID("_FarPlane");

        public FogPass()
        {
            fogMaterial = CoreUtils.CreateEngineMaterial(fogShader);
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            var renderer = renderingData.cameraData.renderer;
            source = renderer.cameraColorTarget;
            
            cmd.GetTemporaryRT(tempHandle, descriptor, FilterMode.Bilinear);
            temp = new RenderTargetIdentifier(tempHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            cmd.Clear();

            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<Fog>();
            
            fogMaterial.SetColor(Color, effect.color.value);
            fogMaterial.SetFloat(Value, effect.density.value);
            fogMaterial.SetFloat(FarPlane, effect.farPlane.value);

            using (new ProfilingScope(cmd, new ProfilingSampler("Fog Pass")))
            {
                Blit(cmd, source, temp, fogMaterial);
                Blit(cmd, temp, source);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }


        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            base.OnCameraCleanup(cmd);
            
            cmd.ReleaseTemporaryRT(tempHandle);
        }
    }
}