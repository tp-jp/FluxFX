using System.Linq;
using TpLab.Flux.FX.Scripts;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Authoring設定とModuleからParticleの論理定義を構築します。
    /// </summary>
    public static class FluxParticleAuthoringDefinitionBuilder
    {
        /// <summary>
        /// Core ValueとModule Definitionを登録します。
        /// </summary>
        /// <param name="authoring">Particleの設定。</param>
        /// <param name="source">Coreの基本定義。</param>
        /// <param name="moduleContext">Moduleが登録した定義と初期値。</param>
        /// <param name="hasRotation">Rotationを使用するか。</param>
        /// <returns>構築した論理定義。</returns>
        public static FluxParticleDefinitionBuilder Create(FluxParticleAuthoring authoring,
            out FluxParticleDefinitionBuilder source, out FluxParticleProgramContribution moduleContext, out bool hasRotation)
        {
            source = FluxParticleCoreDefinitions.Create();
            var definitions = new FluxParticleDefinitionBuilder();
            foreach (var value in source.Values.Values) definitions.AddValue(value);
            moduleContext = new FluxParticleProgramContribution(definitions);
            new FluxParticleModuleRegistry().Contribute(authoring.Modules, moduleContext);
            hasRotation = authoring.Render.StartRotation.z != 0 || definitions.Operations.Values.Any(operation => operation.Writes.Any(write =>
                write.ValueId == "render.rotation"));
            if (hasRotation)
            {
                definitions.AddValue(new FluxParticleValueDefinition("render.rotation", FluxParticleValueType.Float3,
                    FluxParticleWritePolicy.SharedMutable, "fluxfx.core"));
            }
            return definitions;
        }
    }
}
