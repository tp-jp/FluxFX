using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Scripts.Modules;
namespace TpLab.Flux.FX.Editor.Modules
{
    /// <summary>
    /// Moduleハンドルが使用するParameterの対応を定義します。
    /// </summary>
    public interface IFluxParticleModuleBindingDefinition
    {
        /// <summary>
        /// ハンドルのローカル番号順にParameter定義を取得します。
        /// </summary>
        /// <returns>Parameter定義。現在のモードで非対応の要素はnull</returns>
        FluxParticleParameterDefinition[] GetHandleParameters(FluxParticleModule module);

        /// <summary>
        /// EnabledのParameter ID。対応しない場合はnull
        /// </summary>
        string EnabledParameterId { get; }
    }
}
