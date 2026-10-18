using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Inspector;

namespace TpLab.Flux.FX.CustomModules.Editor
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
