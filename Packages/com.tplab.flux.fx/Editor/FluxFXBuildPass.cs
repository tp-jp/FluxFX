using TpLab.Flux.Editor;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
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
            var simulations = Object.FindObjectsOfType<FluxParticleSimulation>(true);

            foreach (var simulation in simulations)
            {
                var gameObject = simulation.gameObject;

                var gravity = gameObject.GetComponent<FluxParticleGravity>();
                var drag = gameObject.GetComponent<FluxParticleDrag>();
                var noise = gameObject.GetComponent<FluxParticleNoise>();
                var vortex = gameObject.GetComponent<FluxParticleVortex>();

                simulation.SetProgramVariable("gravity", gravity != null && gravity.enabled ? gravity.Gravity : Vector3.zero);
                simulation.SetProgramVariable("drag", drag != null && drag.enabled ? drag.Drag : 0.0f);
                simulation.SetProgramVariable("noiseStrength", noise != null && noise.enabled ? noise.Strength : 0.0f);
                simulation.SetProgramVariable("noiseScale", noise != null && noise.enabled ? noise.Scale : 0.0f);
                simulation.SetProgramVariable("noiseSpeed", noise != null && noise.enabled ? noise.Speed : 0.0f);
                simulation.SetProgramVariable("vortexCenter", vortex != null && vortex.enabled ? vortex.Center : Vector3.zero);
                simulation.SetProgramVariable("vortexAxis", vortex != null && vortex.enabled ? vortex.Axis.normalized : Vector3.up);
                simulation.SetProgramVariable("vortexStrength", vortex != null && vortex.enabled ? vortex.Strength : 0.0f);
            }
        }
    }
}