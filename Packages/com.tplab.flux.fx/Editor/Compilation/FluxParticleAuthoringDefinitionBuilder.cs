using System.Linq;
using TpLab.Flux.FX.Udon;
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

        /// <summary>
        /// Authoring設定に応じたCore Providerを登録します。
        /// </summary>
        /// <param name="authoring">Particleの設定。</param>
        /// <param name="source">Coreの基本定義。</param>
        /// <param name="definitions">Providerの登録先。</param>
        public static void AddProviders(FluxParticleAuthoring authoring, FluxParticleDefinitionBuilder source,
            FluxParticleDefinitionBuilder definitions)
        {
            foreach (var provider in source.Providers.Values)
            {
                if (provider.ValueId == "particle.start-color" &&
                    authoring.Render.StartColorMode == FluxParticleStartColorMode.RandomBetweenTwoConstants)
                {
                    definitions.AddProvider(new FluxParticleValueProvider(provider.ValueId, FluxParticleProviderKind.Derived,
                        new[] { "particle.spawn-key" }, new[] { "color.start-min", "color.start-max" },
                        "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringRandomStartColor.hlsl", "FluxFX_StartColor"));
                }
                else
                {
                    definitions.AddProvider(provider.ValueId == "particle.lifetime"
                        ? new FluxParticleValueProvider(provider.ValueId, provider.Kind, provider.Inputs, provider.Parameters,
                            "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringLifetime.hlsl", provider.EntryPoint)
                        : provider);
                }
            }
        }
    }
}
