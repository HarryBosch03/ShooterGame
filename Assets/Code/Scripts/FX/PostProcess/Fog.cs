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

        [Header("Fog Settings")] 
        public FloatParameter density = new(1.0f, true);
        public ColorParameter color = new(Color.gray, true);
    }

    public sealed class FogPass : ScriptableRenderPass
    {
        private readonly Shader fogShader = Shader.Find("Hidden/Fog");
        private readonly Material fogMaterial;

        private CustomRenderFeatures customRenderFeatures;
        
        public FogPass(CustomRenderFeatures customRenderFeatures)
        {
            this.customRenderFeatures = customRenderFeatures;
            
            fogMaterial = CoreUtils.CreateEngineMaterial(fogShader);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<Fog>();

            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, new ProfilingSampler("Fog Pass")))
            {
                var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
                cmd.Blit(cameraColorTarget, cameraColorTarget, fogMaterial);
            }
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(fogMaterial);
        }
    }
}
