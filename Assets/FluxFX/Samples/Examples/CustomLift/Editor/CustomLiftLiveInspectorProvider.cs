using TpLab.Flux.FX.Samples.CustomLift.Udon;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Samples.CustomLift.Scripts;

namespace TpLab.Flux.FX.Samples.CustomLift.Editor
{
    public sealed class CustomLiftLiveInspectorProvider : FluxParticleLiveInspectorProvider
    {
        public override IEnumerable<FluxParticleLiveBinding> Bindings => new[]
        {
            new FluxParticleLiveBinding(typeof(CustomLiftModule), "enabled", CustomLiftContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(CustomLiftModule), "strength", CustomLiftContract.Strength),
            new FluxParticleLiveBinding(typeof(CustomLiftModule), "direction", CustomLiftContract.Direction)
        };
    }
}
