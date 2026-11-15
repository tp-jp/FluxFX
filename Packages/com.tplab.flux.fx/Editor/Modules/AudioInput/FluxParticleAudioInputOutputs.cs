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
        /// 平滑化したFloat3応答のValue ID
        /// </summary>
        public const string Response = "audio-input.response";

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
