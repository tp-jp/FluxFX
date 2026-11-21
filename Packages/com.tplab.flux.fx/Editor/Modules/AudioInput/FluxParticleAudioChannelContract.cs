using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;

namespace TpLab.Flux.FX.Editor.Modules.AudioInput
{
    /// <summary>
    /// 固定IDに対応するチャンネルのLive契約
    /// </summary>
    public sealed class FluxParticleAudioChannelContract
    {
        /// <summary>
        /// 入力元の契約
        /// </summary>
        public FluxParticleParameterContract<FluxParticleAudioSource> Source { get; }

        /// <summary>
        /// 追加感度の契約
        /// </summary>
        public FluxParticleParameterContract<float> Gain { get; }

        /// <summary>
        /// 立ち上がり時間の契約
        /// </summary>
        public FluxParticleParameterContract<float> Attack { get; }

        /// <summary>
        /// 減衰時間の契約
        /// </summary>
        public FluxParticleParameterContract<float> Release { get; }

        /// <summary>
        /// Runtimeのenum順の契約一覧
        /// </summary>
        public IReadOnlyList<FluxParticleParameterContract> Parameters { get; }

        /// <summary>
        /// 固定IDからParameterの契約を作成します。
        /// </summary>
        /// <param name="id">チャンネルの固定ID</param>
        public FluxParticleAudioChannelContract(string id)
        {
            RequireId(id);
            var prefix = "audio-input.channel." + id;
            Source = FluxParticleParameter.Enum<FluxParticleAudioSource>(prefix + ".source")
                .AllowedValues(FluxParticleAudioSource.Average, FluxParticleAudioSource.Bass, FluxParticleAudioSource.LowMid,
                    FluxParticleAudioSource.HighMid, FluxParticleAudioSource.Treble)
                .ApiName("Source").Policy(FluxParticleParameterPolicy.Live).Build();
            Gain = FluxParticleParameter.Float(prefix + ".gain").Range(0, 4).ApiName("Gain").Policy(FluxParticleParameterPolicy.Live).Build();
            Attack = FluxParticleParameter.Float(prefix + ".attack").Minimum(0.01f).ApiName("Attack").Policy(FluxParticleParameterPolicy.Live).Build();
            Release = FluxParticleParameter.Float(prefix + ".release").Minimum(0.01f).ApiName("Release").Policy(FluxParticleParameterPolicy.Live).Build();
            var parameters = new FluxParticleParameterContract[4];
            parameters[(int)FluxParticleAudioChannelParameter.Source] = Source;
            parameters[(int)FluxParticleAudioChannelParameter.Gain] = Gain;
            parameters[(int)FluxParticleAudioChannelParameter.Attack] = Attack;
            parameters[(int)FluxParticleAudioChannelParameter.Release] = Release;
            Parameters = Array.AsReadOnly(parameters);
        }

        /// <summary>
        /// 空IDや重複を拒否します。保存済みIDの自動修復は行いません。
        /// </summary>
        public static void Validate(IReadOnlyList<FluxParticleAudioChannel> channels)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var channel in channels)
            {
                if (channel == null) throw new InvalidOperationException("Audio Input contains a null channel.");
                RequireId(channel.Id);
                if (!ids.Add(channel.Id)) throw new InvalidOperationException("Duplicate Audio Input channel ID: " + channel.Id);
            }
        }

        internal static void RequireId(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Audio Input channel ID is required.");
            foreach (var c in id)
            {
                if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9'))
                    throw new ArgumentException("Audio Input channel ID must contain only lowercase ASCII letters and digits: " + id);
            }
        }
    }
}
