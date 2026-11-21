using System;
using TpLab.Flux.FX.Editor.Compilation.Definitions;

namespace TpLab.Flux.FX.Editor.Modules.AudioInput
{
    /// <summary>
    /// 他Module向けの音声出力契約。Float3のx=選択音域の速い応答、y=遅い応答、z=全4音域平均の応答。
    /// 各成分は0〜1。Spawnでゼロから開始し、生存粒子のUpdateで平滑化します。
    /// </summary>
    public static class FluxParticleAudioInputOutputs
    {
        /// <summary>
        /// 未平滑化のFloat3入力。x=選択音域、y=全4音域平均、z=更新カウンター（未取得は-1）。
        /// Gain・NoiseFloor・Strength・AverageGain適用後、Attack／Release適用前の値。
        /// </summary>
        public const string Sample = "audio-input.sample";

        /// <summary>
        /// 共通入力処理後・平滑化前の4帯域。xyzw= Bass／LowMid／HighMid／Treble。
        /// 有効性はSample.zとFreshness、Enabled、Timeoutで判定します。
        /// </summary>
        public const string Bands = "audio-input.bands";

        /// <summary>
        /// AudioLinkから取得した未加工の4帯域。通常の利用側はBandsまたはSampleを使います。
        /// </summary>
        public const string RawBands = "audio-input.raw-bands";

        /// <summary>
        /// 共通更新カウンター。入力未取得時は-1
        /// </summary>
        public const string Frame = "audio-input.frame";

        /// <summary>
        /// 共通入力処理後・平滑化前の4帯域を読み取ります。
        /// </summary>
        public static FluxParticleValueRead ReadBands() => new FluxParticleValueRead(Bands, FluxParticleReadVersion.Provided);

        /// <summary>
        /// Float2の入力鮮度。x=最後のカウンター、y=変化がない秒数。
        /// 利用側はEnabled、x >= 0、y &lt; Timeoutを合わせて判定します。
        /// </summary>
        public const string Freshness = "audio-input.freshness";

        /// <summary>
        /// 入力鮮度を確定するOperation ID
        /// </summary>
        public const string UpdateFreshness = "audio-input.update-freshness";

        /// <summary>
        /// Updateで独立した応答を計算するための未平滑化入力の読み取り契約
        /// </summary>
        public static FluxParticleValueRead ReadSample() => new FluxParticleValueRead(Sample, FluxParticleReadVersion.Provided);

        /// <summary>
        /// 当該Stepの鮮度を読み取り、更新Operationへの依存を宣言します。
        /// </summary>
        public static FluxParticleValueRead ReadFreshness() => new FluxParticleValueRead(Freshness,
            FluxParticleReadVersion.AfterOperation, UpdateFreshness);

        /// <summary>
        /// 平滑化したFloat3応答のValue ID
        /// </summary>
        public const string Response = "audio-input.response";

        /// <summary>
        /// 固定IDに対応するチャンネルの平滑化済みFloat応答
        /// </summary>
        public static string ChannelResponse(string id)
        {
            FluxParticleAudioChannelContract.RequireId(id);
            return "audio-input.channel." + id + ".response";
        }

        /// <summary>
        /// チャンネル感度適用後・平滑化前のFloat入力
        /// </summary>
        public static string ChannelSample(string id)
        {
            FluxParticleAudioChannelContract.RequireId(id);
            return "audio-input.channel." + id + ".sample";
        }

        /// <summary>
        /// チャンネルの更新Operation ID
        /// </summary>
        public static string ChannelUpdate(string id) => ChannelResponse(id) + ".update";

        /// <summary>
        /// Updateでは当該Stepの応答、Renderでは確定済み応答を読み取ります。
        /// 存在しないIDはビルドエラーとなり、別チャンネルへの置換は行いません。
        /// </summary>
        public static FluxParticleValueRead ReadChannel(string id, FluxParticlePhase phase)
        {
            var response = ChannelResponse(id);
            if (phase == FluxParticlePhase.Render) return new FluxParticleValueRead(response, FluxParticleReadVersion.Committed);
            if (phase == FluxParticlePhase.Update) return new FluxParticleValueRead(response, FluxParticleReadVersion.AfterOperation, ChannelUpdate(id));
            throw new ArgumentOutOfRangeException(nameof(phase), "Audio response is initialized to zero at Spawn.");
        }

        /// <summary>
        /// 当該Stepの応答を確定するUpdate Operation ID
        /// </summary>
        public const string UpdateOperation = "audio-input.update-response";

        /// <summary>
        /// Updateでは当該Stepの更新後、Renderでは確定済みの応答を読み取ります。
        /// </summary>
        /// <param name="phase">利用側OperationのPhase。Spawnは非対応</param>
        /// <returns>利用側Contribution.Operationへ渡す読み取り契約</returns>
        public static FluxParticleValueRead Read(FluxParticlePhase phase)
        {
            if (phase == FluxParticlePhase.Render)
            {
                return new FluxParticleValueRead(Response, FluxParticleReadVersion.Committed);
            }
            if (phase == FluxParticlePhase.Update)
            {
                return new FluxParticleValueRead(Response, FluxParticleReadVersion.AfterOperation, UpdateOperation);
            }
            throw new ArgumentOutOfRangeException(nameof(phase), "Audio response is initialized to zero at Spawn.");
        }
    }
}
