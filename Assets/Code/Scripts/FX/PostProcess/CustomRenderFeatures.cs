using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bosch.FX.PostProcess
{
    public sealed class CustomRenderFeatures : ScriptableRendererFeature
    {
        private FogPass fogPass;

        public RenderTargetIdentifier Src { get; private set; }
        public RenderTargetIdentifier Dst { get; private set; }
        
        public override void Create()
        {
            fogPass = new FogPass(this);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Src = renderer.cameraColorTarget;
            
            renderer.EnqueuePass(fogPass);
        }

        protected override void Dispose(bool disposing)
        {
            fogPass.Cleanup();
        }
    }
}
