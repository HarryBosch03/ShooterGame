using UnityEngine.Rendering.Universal;

namespace Bosch.FX.PostProcess
{
    public sealed class CustomRenderFeatures : ScriptableRendererFeature
    {
        private FogPass fogPass;

        public override void Create()
        {
            fogPass = new FogPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(fogPass);
        }
    }
}
