using System;
using TpLab.Flux.FX.Editor.Compilation.Contracts;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Modules.AudioInput
{
    /// <summary>
    /// 設定されたチャンネルだけに独立した応答状態を追加します。
    /// </summary>
    static class FluxParticleAudioChannelBuilder
    {
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Modules/AudioInput/Channel.hlsl";

        internal static void Contribute(FluxParticleAudioInputModule module, FluxParticleProgramContribution context)
        {
            FluxParticleAudioChannelContract.Validate(module.Channels);
            foreach (var channel in module.Channels)
            {
                var contract = new FluxParticleAudioChannelContract(channel.Id);
                Bind(context, contract.Source, channel.Source);
                Bind(context, contract.Gain, channel.Gain);
                Bind(context, contract.Attack, channel.Attack);
                Bind(context, contract.Release, channel.Release);
                var response = FluxParticleAudioInputOutputs.ChannelResponse(channel.Id);
                var sample = FluxParticleAudioInputOutputs.ChannelSample(channel.Id);
                var update = FluxParticleAudioInputOutputs.ChannelUpdate(channel.Id);
                var owner = FluxParticleAudioInputContract.Module.Id;
                context.AddValue(new FluxParticleValueDefinition(sample, FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly, owner));
                context.AddProvider(new FluxParticleValueProvider(sample, FluxParticleProviderKind.Derived,
                    new[] { FluxParticleAudioInputOutputs.Bands, FluxParticleAudioInputOutputs.Sample },
                    new[] { contract.Source.Id, contract.Gain.Id }, Hlsl, "FluxFX_AudioChannelSample"));
                context.AddValue(new FluxParticleValueDefinition(response, FluxParticleValueType.Float, FluxParticleWritePolicy.OwnerManaged, owner));
                context.AddOperation(new FluxParticleOperationDefinition(response + ".initialize", owner, FluxParticlePhase.Spawn,
                    Array.Empty<FluxParticleValueRead>(), new[] { new FluxParticleValueWrite(response, FluxParticleWriteKind.Initialize) },
                    Array.Empty<string>(), Hlsl, "FluxFX_AudioChannelInitialize", useValueArguments: true));
                context.AddOperation(new FluxParticleOperationDefinition(update, owner, FluxParticlePhase.Update,
                    new[] { new FluxParticleValueRead(response, FluxParticleReadVersion.StepPrevious),
                        FluxParticleAudioInputOutputs.ReadFreshness(), new FluxParticleValueRead(sample, FluxParticleReadVersion.Provided),
                        new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided) },
                    new[] { new FluxParticleValueWrite(response, FluxParticleWriteKind.Modify) },
                    new[] { FluxParticleAudioInputOutputs.UpdateFreshness }, Hlsl, "FluxFX_AudioChannelUpdate",
                    new[] { FluxParticleAudioInputContract.Module.Enabled.Id, FluxParticleAudioInputContract.Timeout.Id,
                        contract.Attack.Id, contract.Release.Id }, useValueArguments: true));
            }
        }

        static void Bind<T>(FluxParticleProgramContribution context, FluxParticleParameterContract<T> contract, T value) where T : struct
        {
            context.AddParameter(FluxParticleParameterContractCompiler.Compile(contract), FluxParticleParameterContractCompiler.Encode(contract, value));
        }
    }
}
