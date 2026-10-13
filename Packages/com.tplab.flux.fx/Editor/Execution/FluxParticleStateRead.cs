using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Execution
{
    /// <summary>
    /// OperationがParticleの状態を読み取る時点を表します。
    /// GPU Textureの物理的な割り当てやBuffer数は定義しません。
    /// </summary>
    public enum FluxParticleStateReadSource
    {
        /// <summary>
        /// フレーム開始時に確定していたParticleの状態を読み取ります。
        /// </summary>
        Previous,

        /// <summary>
        /// 同一Pass内で現在までに更新された状態を読み取ります。
        /// 読み取り対象はOperation自身のTargetに属する属性に限定されます。
        /// </summary>
        Working,

        /// <summary>
        /// このOperationより先に完了した別Passの更新結果を読み取ります。
        /// </summary>
        Updated
    }

    /// <summary>
    /// Operationによる論理Particle属性の読み取り契約を定義します。
    /// 属性と読み取り時点を個別に指定することで、Pass間の依存関係を検証できます。
    /// </summary>
    public sealed class FluxParticleStateRead
    {
        /// <summary>
        /// 読み取る論理Particle属性です。
        /// 単一の属性を指定します。
        /// </summary>
        public FluxParticleAttribute Attribute { get; }

        /// <summary>
        /// 読み取りに使用する状態の時点です。
        /// </summary>
        public FluxParticleStateReadSource Source { get; }

        /// <summary>
        /// 属性と読み取り時点を組み合わせた読み取り契約を作成します。
        /// 有効な属性および実行順序との整合性はExecutionPlanで検証します。
        /// </summary>
        /// <param name="attribute">読み取る単一のParticle属性。</param>
        /// <param name="source">読み取り対象となる状態の時点。</param>
        public FluxParticleStateRead(
            FluxParticleAttribute attribute,
            FluxParticleStateReadSource source)
        {
            Attribute = attribute;
            Source = source;
        }
    }
}