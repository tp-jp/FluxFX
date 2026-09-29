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

                simulation.SetProgramVariable("gravity", gravity != null ? gravity.Gravity : Vector3.zero);
                simulation.SetProgramVariable("drag", drag != null ? drag.Drag : 0.0f);
                simulation.SetProgramVariable("noiseStrength", noise != null ? noise.Strength : 0.0f);
                simulation.SetProgramVariable("noiseScale", noise != null ? noise.Scale : 0.0f);
                simulation.SetProgramVariable("noiseSpeed", noise != null ? noise.Speed : 0.0f);
            }            
        }
    }
}
