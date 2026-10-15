using System;
using TpLab.Flux.FX.Udon;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Particle Systemが必須とするCore Value、Provider、Spawn、Renderの定義を構築します。
    /// </summary>
    public static class FluxParticleCoreDefinitions
    {
        const string Core = "fluxfx.core";
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Core/Hlsl/";

        /// <summary>
        /// ModuleのContributionを追加する前のCore定義を構築します。
        /// </summary>
        public static FluxParticleDefinitionBuilder Create()
        {
            var builder = new FluxParticleDefinitionBuilder();
            AddValue(builder, "particle.position", FluxParticleValueType.Float3, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "particle.age", FluxParticleValueType.Float, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.velocity", FluxParticleValueType.Float3, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "particle.spawn-key", FluxParticleValueType.UInt, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.lifetime", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "particle.start-color", FluxParticleValueType.Float4, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "particle.start-size", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "render.size", FluxParticleValueType.Float, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "render.uv-transform", FluxParticleValueType.Float4, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "render.color", FluxParticleValueType.Float4, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "step.time", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "step.delta-time", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "spawn.key", FluxParticleValueType.UInt, FluxParticleWritePolicy.ReadOnly);

            builder.AddProvider(new FluxParticleValueProvider("step.delta-time", FluxParticleProviderKind.Uniform));
            builder.AddProvider(new FluxParticleValueProvider("spawn.key", FluxParticleProviderKind.SpawnInput));
            AddParameter(builder, "spawn.position", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live);
            AddParameter(builder, "spawn.velocity", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live);
            AddParameter(builder, "lifetime.range", FluxParticleValueType.Float2, FluxParticleParameterPolicy.Static);
            AddParameter(builder, "color.start", FluxParticleValueType.Float4, FluxParticleParameterPolicy.Static);
            AddParameter(builder, "color.end", FluxParticleValueType.Float4, FluxParticleParameterPolicy.Live);

            builder.AddProvider(new FluxParticleValueProvider("particle.lifetime", FluxParticleProviderKind.Derived,
                new[] { "particle.spawn-key" }, new[] { "lifetime.range" }, Hlsl + "Lifetime.hlsl", "FluxFX_Lifetime"));
            builder.AddProvider(new FluxParticleValueProvider("particle.start-color", FluxParticleProviderKind.Derived,
                parameters: new[] { "color.start" }, hlslPath: Hlsl + "StartColor.hlsl", entryPoint: "FluxFX_StartColor"));

            builder.AddOperation(new FluxParticleOperationDefinition("spawn.position", Core, FluxParticlePhase.Spawn,
                Array.Empty<FluxParticleValueRead>(), new[] { Init("particle.position"), Init("particle.age") }, null,
                Hlsl + "SpawnPosition.hlsl", "FluxFX_SpawnPosition", new[] { "spawn.position" }));
            builder.AddOperation(new FluxParticleOperationDefinition("spawn.velocity", Core, FluxParticlePhase.Spawn,
                new[] { Read("spawn.key", FluxParticleReadVersion.Provided) },
                new[] { Init("particle.velocity"), Init("particle.spawn-key") }, null,
                Hlsl + "SpawnVelocity.hlsl", "FluxFX_SpawnVelocity", new[] { "spawn.velocity" }));
            builder.AddOperation(new FluxParticleOperationDefinition("render.start", Core, FluxParticlePhase.Render,
                new[] { Read("particle.start-color", FluxParticleReadVersion.Provided) },
                new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Replace) }, null,
                Hlsl + "RenderStart.hlsl", "FluxFX_RenderStart"));
            builder.AddOperation(new FluxParticleOperationDefinition("render.uv-start", Core, FluxParticlePhase.Render,
                Array.Empty<FluxParticleValueRead>(),
                new[] { new FluxParticleValueWrite("render.uv-transform", FluxParticleWriteKind.Replace) }, null,
                Hlsl + "RenderUvStart.hlsl", "FluxFX_RenderUvStart"));
            return builder;
        }

        /// <summary>
        /// Core Particleの生存判定に使用するValue IDを返します。
        /// </summary>
        public static FluxParticleShaderLifecycle CreateLifecycle() => new FluxParticleShaderLifecycle("particle.age", "particle.lifetime");

        static void AddValue(FluxParticleDefinitionBuilder builder, string id, FluxParticleValueType type, FluxParticleWritePolicy policy)
        {
            builder.AddValue(new FluxParticleValueDefinition(id, type, policy, Core,
                type == FluxParticleValueType.UInt ? 1048575u : (uint?)null));
        }

        static void AddParameter(FluxParticleDefinitionBuilder builder, string id, FluxParticleValueType type, FluxParticleParameterPolicy policy)
        {
            builder.AddParameter(new FluxParticleParameterDefinition(id, type, policy));
        }

        static FluxParticleValueRead Read(string id, FluxParticleReadVersion version) => new FluxParticleValueRead(id, version);
        static FluxParticleValueWrite Init(string id) => new FluxParticleValueWrite(id, FluxParticleWriteKind.Initialize);
    }
}
