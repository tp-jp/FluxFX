using TpLab.Flux.Editor;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using UdonSharpEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor
{
    public class FluxFXBuildPass : PassBase
    {
        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.After<FluxBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            var authorings = Object.FindObjectsOfType<FluxParticleAuthoring>(true);
            var compiler = new FluxParticleCompiler();
            var lutBaker = new FluxParticleLutBaker();

            foreach (var authoring in authorings)
            {
                var particleSystem = authoring.GetComponent<FluxParticleSystem>();
                if (particleSystem == null) continue;

                var compiledParameters = compiler.Compile(authoring);
                particleSystem.SetProgramVariable("compiledParameters", compiledParameters);

                var particleRenderer = authoring.GetComponentInChildren<FluxParticleRenderer>(true);
                if (particleRenderer == null)
                {
                    Debug.LogError($"[FluxFX] FluxParticleRenderer was not found under '{authoring.name}'.", authoring);
                    continue;
                }

                var renderMode = authoring.Render.Mode;
                var sourceMesh = authoring.Render.Mesh;
                var particleMaterial = authoring.Render.Material;
                var colorOverLifetimeLut = lutBaker.BakeColorOverLifetime(authoring);
                var sizeOverLifetimeLut = lutBaker.BakeSizeOverLifetime(authoring);

                particleRenderer.SetProgramVariable("renderMode", renderMode);
                particleRenderer.SetProgramVariable("sourceMesh", sourceMesh);
                particleRenderer.SetProgramVariable("particleMaterial", particleMaterial);
                particleRenderer.SetProgramVariable("colorOverLifetimeLut", colorOverLifetimeLut);
                particleRenderer.SetProgramVariable("sizeOverLifetimeLut", sizeOverLifetimeLut);
            }
        }
    }
}