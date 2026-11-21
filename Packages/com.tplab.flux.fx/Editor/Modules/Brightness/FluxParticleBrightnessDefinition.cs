using System.Linq;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Modules.AudioInput;
using TpLab.Flux.FX.Editor.Modules.Shared;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.Brightness
{
    /// <summary>
    /// 色処理の結果へ明るさを適用し、必要な構成に限って独立した音声応答を生成します。
    /// 独自の色処理は、このModuleより先に配置します。音声増光にはAudioInputが必要です。
    /// </summary>
    public sealed class FluxParticleBrightnessDefinition : FluxParticleModuleDefinition<FluxParticleBrightnessModule>
    {
        const string Root = "Packages/com.tplab.flux.fx/Editor/Modules/Brightness/";

        /// <inheritdoc />
        public override string ModuleId => FluxParticleBrightnessContract.Module.Id;

        /// <inheritdoc />
        public override void Contribute(FluxParticleBrightnessModule module, FluxParticleProgramContribution context)
        {
            var contribution = new FluxParticleModuleContribution(context, FluxParticleBrightnessContract.Module, module.Enabled);
            var renderParameters = new[]
            {
                contribution.Bind(FluxParticleBrightnessContract.BaseMinimum, module.BaseMinimum),
                contribution.Bind(FluxParticleBrightnessContract.BaseMaximum, module.BaseMaximum),
                contribution.Bind(FluxParticleBrightnessContract.Waveform, module.Waveform),
                contribution.Bind(FluxParticleBrightnessContract.Period, module.Period),
                contribution.Bind(FluxParticleBrightnessContract.PeriodMinimum, module.PeriodMinimum),
                contribution.Bind(FluxParticleBrightnessContract.PeriodMaximum, module.PeriodMaximum),
                contribution.Bind(FluxParticleBrightnessContract.RandomPhase, module.RandomPhase),
                contribution.Bind(FluxParticleBrightnessContract.TimeMode, module.TimeMode),
                contribution.Bind(FluxParticleBrightnessContract.Minimum, module.Minimum),
                contribution.Bind(FluxParticleBrightnessContract.Maximum, module.Maximum),
                contribution.Bind(FluxParticleBrightnessContract.Duty, module.Duty),
                contribution.Bind(FluxParticleBrightnessContract.Smoothness, module.Smoothness),
                contribution.Bind(FluxParticleBrightnessContract.Target, module.Target),
                contribution.Bind(FluxParticleBrightnessContract.AudioBoost, module.AudioBoost)
            };
            var attack = contribution.Bind(FluxParticleBrightnessContract.AudioAttack, module.AudioAttack);
            var release = contribution.Bind(FluxParticleBrightnessContract.AudioRelease, module.AudioRelease);
            var source = contribution.Bind(FluxParticleBrightnessContract.AudioSource, module.AudioSource);
            if (!context.Providers.ContainsKey("step.time"))
            {
                context.AddProvider(new FluxParticleValueProvider("step.time", FluxParticleProviderKind.Uniform));
            }
            context.AddValue(new FluxParticleValueDefinition("brightness.audio", FluxParticleValueType.Float,
                module.AudioEnabled && string.IsNullOrEmpty(module.AudioChannel) ? FluxParticleWritePolicy.OwnerManaged : FluxParticleWritePolicy.ReadOnly, ModuleId));
            if (module.AudioEnabled && string.IsNullOrEmpty(module.AudioChannel))
            {
                // References are resolved after all modules contribute, regardless of AudioInput registration order.
                contribution.Operation("brightness.initialize", FluxParticlePhase.Spawn,
                    new FluxParticleValueRead[0],
                    new[] { new FluxParticleValueWrite("brightness.audio", FluxParticleWriteKind.Initialize) },
                    Root + "Initialize.hlsl", "FluxFX_BrightnessInitialize", false);
                contribution.Operation("brightness.update", FluxParticlePhase.Update,
                    new[]
                    {
                        new FluxParticleValueRead("brightness.audio", FluxParticleReadVersion.StepPrevious),
                        FluxParticleAudioInputOutputs.ReadSample(), FluxParticleAudioInputOutputs.ReadBands(), FluxParticleAudioInputOutputs.ReadFreshness(),
                        new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                    },
                    new[] { new FluxParticleValueWrite("brightness.audio", FluxParticleWriteKind.Modify) },
                    Root + "Audio.hlsl", "FluxFX_BrightnessUpdate", false,
                    new[] { attack, release, source, FluxParticleBrightnessContract.Module.Enabled.Id,
                        FluxParticleAudioInputContract.Module.Enabled.Id, FluxParticleAudioInputContract.Timeout.Id });
            }
            else if (module.AudioEnabled)
            {
                // A channel already owns its response envelope. Do not filter it a second time.
                context.AddProvider(new FluxParticleValueProvider("brightness.audio", FluxParticleProviderKind.Derived,
                    new[] { FluxParticleAudioInputOutputs.ChannelResponse(module.AudioChannel) },
                    hlslPath: Root + "Channel.hlsl", entryPoint: "FluxFX_BrightnessChannel"));
            }
            else
            {
                context.AddProvider(new FluxParticleValueProvider("brightness.audio", FluxParticleProviderKind.Derived,
                    hlslPath: Root + "NoAudio.hlsl", entryPoint: "FluxFX_BrightnessNoAudio"));
            }
            var predecessors = context.Operations.Values.Where(operation => operation.Phase == FluxParticlePhase.Render &&
                    operation.Writes.Any(write => write.ValueId == "render.color"))
                .Select(operation => operation.Id).Concat(new[] { "render.color" }).Distinct().ToArray();
            contribution.Operation("render.brightness", FluxParticlePhase.Render,
                new[]
                {
                    new FluxParticleValueRead("render.color", FluxParticleReadVersion.AfterOperation, "render.start"),
                    new FluxParticleValueRead("particle.spawn-key", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.Committed),
                    new FluxParticleValueRead("step.time", FluxParticleReadVersion.Provided),
                    new FluxParticleValueRead("brightness.audio", module.AudioEnabled && string.IsNullOrEmpty(module.AudioChannel) ? FluxParticleReadVersion.Committed : FluxParticleReadVersion.Provided)
                }, new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Modify) },
                Root + "Render.hlsl", module.PreserveBlinkPhase ? "FluxFX_BrightnessRenderWithBlinkPhase" : "FluxFX_BrightnessRender",
                true, renderParameters, optionalDependencies: predecessors);
            contribution.Complete();
        }
    }
}
