using System.IO;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEditor.PackageManager;

namespace TpLab.Flux.FX.Editor.Modules.AudioInput
{
    /// <summary>
    /// 任意依存のAudioLinkから入力を読み取り、粒子ごとの応答を更新します。
    /// </summary>
    public sealed class FluxParticleAudioInputDefinition : FluxParticleModuleDefinition<FluxParticleAudioInputModule>
    {
        const string Root = "Packages/com.tplab.flux.fx/Editor/Modules/AudioInput/";
        const string AudioLinkInclude = "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc";
        internal const string Freshness = "audio-input.freshness";
        internal const string Sample = "audio-input.sample";

        /// <inheritdoc />
        public override string ModuleId => FluxParticleAudioInputContract.Module.Id;

        /// <summary>
        /// ビルドとInspectorで共有するAudioLinkシェーダーの導入判定
        /// </summary>
        internal static bool IsAudioLinkInstalled
        {
            get
            {
                var package = PackageInfo.FindForAssetPath(AudioLinkInclude);
                return package != null && File.Exists(Path.Combine(package.resolvedPath, "Runtime/Shaders/AudioLink.cginc"));
            }
        }

        /// <inheritdoc />
        public override void Contribute(FluxParticleAudioInputModule module, FluxParticleProgramContribution context)
        {
            var contribution = new FluxParticleModuleContribution(context, FluxParticleAudioInputContract.Module, module.Enabled);
            var band = contribution.Bind(FluxParticleAudioInputContract.Band, module.Band);
            var gain = contribution.Bind(FluxParticleAudioInputContract.Gain, module.Gain);
            var noiseFloor = contribution.Bind(FluxParticleAudioInputContract.NoiseFloor, module.NoiseFloor);
            var strength = contribution.Bind(FluxParticleAudioInputContract.Strength, module.Strength);
            var attack = contribution.Bind(FluxParticleAudioInputContract.Attack, module.Attack);
            var release = contribution.Bind(FluxParticleAudioInputContract.Release, module.Release);
            var slowResponse = contribution.Bind(FluxParticleAudioInputContract.SlowResponse, module.SlowResponse);
            var averageGain = contribution.Bind(FluxParticleAudioInputContract.AverageGain, module.AverageGain);
            var timeout = contribution.Bind(FluxParticleAudioInputContract.Timeout, module.Timeout);
            context.AddValue(new FluxParticleValueDefinition(Sample, FluxParticleValueType.Float3,
                FluxParticleWritePolicy.ReadOnly, ModuleId));
            context.AddValue(new FluxParticleValueDefinition(Freshness, FluxParticleValueType.Float2,
                FluxParticleWritePolicy.OwnerManaged, ModuleId));
            context.AddValue(new FluxParticleValueDefinition(FluxParticleAudioInputOutputs.Response, FluxParticleValueType.Float3,
                FluxParticleWritePolicy.OwnerManaged, ModuleId));
            context.AddProvider(new FluxParticleValueProvider(Sample, FluxParticleProviderKind.Derived,
                parameters: new[] { band, gain, noiseFloor, strength, averageGain },
                hlslPath: Root + (IsAudioLinkInstalled ? "AudioLinkInput.hlsl" : "NoAudioInput.hlsl"),
                entryPoint: "FluxFX_AudioInputSample"));
            contribution.Operation("audio-input.initialize", FluxParticlePhase.Spawn,
                new FluxParticleValueRead[0],
                new[]
                {
                    new FluxParticleValueWrite(Freshness, FluxParticleWriteKind.Initialize),
                    new FluxParticleValueWrite(FluxParticleAudioInputOutputs.Response, FluxParticleWriteKind.Initialize)
                }, Root + "Initialize.hlsl", "FluxFX_AudioInputInitialize", false);
            // OFF must keep both operations running so the response can decay.
            contribution.Operation("audio-input.update-freshness", FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead(Freshness, FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead(Sample, FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite(Freshness, FluxParticleWriteKind.Modify) },
                Root + "Freshness.hlsl", "FluxFX_AudioInputFreshness", false, new[] { timeout });
            contribution.Operation(FluxParticleAudioInputOutputs.UpdateOperation, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead(FluxParticleAudioInputOutputs.Response, FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead(Freshness, FluxParticleReadVersion.AfterOperation, "audio-input.update-freshness"),
                    new FluxParticleValueRead(Sample, FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite(FluxParticleAudioInputOutputs.Response, FluxParticleWriteKind.Modify) },
                Root + "Response.hlsl", "FluxFX_AudioInputResponse", false,
                new[] { attack, release, slowResponse, timeout, FluxParticleAudioInputContract.Module.Enabled.Id });
            contribution.Complete();
        }
    }
}
