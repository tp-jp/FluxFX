using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules.Brightness
{
    /// <summary>
    /// BrightnessのInspectorをLive契約へ接続します。
    /// </summary>
    public sealed class FluxParticleBrightnessLiveInspectorProvider : FluxParticleLiveInspectorProvider
    {
        /// <inheritdoc />
        public override IEnumerable<FluxParticleLiveBinding> Bindings => new[]
        {
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "enabled", FluxParticleBrightnessContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "baseMinimum", FluxParticleBrightnessContract.BaseMinimum),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "baseMaximum", FluxParticleBrightnessContract.BaseMaximum),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "waveform", FluxParticleBrightnessContract.Waveform),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "period", FluxParticleBrightnessContract.Period),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "periodMinimum", FluxParticleBrightnessContract.PeriodMinimum),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "periodMaximum", FluxParticleBrightnessContract.PeriodMaximum),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "randomPhase", FluxParticleBrightnessContract.RandomPhase),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "timeMode", FluxParticleBrightnessContract.TimeMode),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "minimum", FluxParticleBrightnessContract.Minimum),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "maximum", FluxParticleBrightnessContract.Maximum),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "duty", FluxParticleBrightnessContract.Duty),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "smoothness", FluxParticleBrightnessContract.Smoothness),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "target", FluxParticleBrightnessContract.Target),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "audioBoost", FluxParticleBrightnessContract.AudioBoost),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "audioAttack", FluxParticleBrightnessContract.AudioAttack),
            new FluxParticleLiveBinding(typeof(FluxParticleBrightnessModule), "audioRelease", FluxParticleBrightnessContract.AudioRelease)
        };
    }
}
