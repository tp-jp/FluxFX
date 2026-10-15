namespace TpLab.Flux.FX.Editor.Compilation.Modules
{
    /// <summary>
    /// Moduleハンドルが使用するParameterの対応を定義します。
    /// </summary>
    public interface IFluxParticleModuleBindingDefinition
    {
        /// <summary>
        /// ハンドルのローカル番号順のParameter定義
        /// </summary>
        FluxParticleParameterDefinition[] HandleParameters { get; }

        /// <summary>
        /// EnabledのParameter ID。対応しない場合はnull
        /// </summary>
        string EnabledParameterId { get; }
    }
}
