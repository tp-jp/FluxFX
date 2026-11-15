using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Inspector;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Modules
{
    /// <summary>
    /// 音声入力と距離フェードのInspectorをLive契約へ接続します。
    /// </summary>
    public sealed class FluxParticleInputAppearanceLiveInspectorProvider : FluxParticleLiveInspectorProvider
    {
        /// <inheritdoc />
        public override IEnumerable<FluxParticleLiveBinding> Bindings => new[]
        {
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "enabled", AudioInput.FluxParticleAudioInputContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "band", AudioInput.FluxParticleAudioInputContract.Band),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "gain", AudioInput.FluxParticleAudioInputContract.Gain),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "noiseFloor", AudioInput.FluxParticleAudioInputContract.NoiseFloor),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "strength", AudioInput.FluxParticleAudioInputContract.Strength),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "attack", AudioInput.FluxParticleAudioInputContract.Attack),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "release", AudioInput.FluxParticleAudioInputContract.Release),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "slowResponse", AudioInput.FluxParticleAudioInputContract.SlowResponse),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "averageGain", AudioInput.FluxParticleAudioInputContract.AverageGain),
            new FluxParticleLiveBinding(typeof(FluxParticleAudioInputModule), "timeout", AudioInput.FluxParticleAudioInputContract.Timeout),
            new FluxParticleLiveBinding(typeof(FluxParticleDistanceFadeModule), "enabled", DistanceFade.FluxParticleDistanceFadeContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleDistanceFadeModule), "startDistance", DistanceFade.FluxParticleDistanceFadeContract.StartDistance),
            new FluxParticleLiveBinding(typeof(FluxParticleDistanceFadeModule), "endDistance", DistanceFade.FluxParticleDistanceFadeContract.EndDistance),
            new FluxParticleLiveBinding(typeof(FluxParticleDistanceFadeModule), "target", DistanceFade.FluxParticleDistanceFadeContract.Target)
        };
    }
}
