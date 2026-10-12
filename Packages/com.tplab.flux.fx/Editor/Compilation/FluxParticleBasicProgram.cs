using System;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// 新Backendの検証用に、2Page/2Passの基本Particle宣言を構築します。
    /// 既存Module Registryや固定Stageを参照せず、HLSL実装を各宣言に登録します。
    /// </summary>
    public static class FluxParticleBasicProgram
    {
        const string Core = "fluxfx.core";
        const string Hlsl = "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/";

        /// <summary>

        /// Position/Age・Velocity/SpawnKeyと、RTを使わないColor評価の定義を構築します。

        /// </summary>
        /// <returns>
        /// Shader Generatorへ渡せる独立した定義集合。
        /// </returns>
        public static FluxParticleDefinitionBuilder CreateDefinitions()
        {
            var builder = new FluxParticleDefinitionBuilder();
            AddValue(builder, "particle.position", FluxParticleValueType.Float3, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.age", FluxParticleValueType.Float, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.velocity", FluxParticleValueType.Float3, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "particle.spawn-key", FluxParticleValueType.UInt, FluxParticleWritePolicy.SystemManaged);
            AddValue(builder, "particle.lifetime", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "particle.start-color", FluxParticleValueType.Float4, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "render.color", FluxParticleValueType.Float4, FluxParticleWritePolicy.SharedMutable);
            AddValue(builder, "step.delta-time", FluxParticleValueType.Float, FluxParticleWritePolicy.ReadOnly);
            AddValue(builder, "spawn.key", FluxParticleValueType.UInt, FluxParticleWritePolicy.ReadOnly);
            builder.AddProvider(new FluxParticleValueProvider("step.delta-time", FluxParticleProviderKind.Uniform));
            builder.AddProvider(new FluxParticleValueProvider("spawn.key", FluxParticleProviderKind.SpawnInput));
            AddParameter(builder, "spawn.position", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live);
            AddParameter(builder, "spawn.velocity", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live);
            AddParameter(builder, "gravity", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live);
            AddParameter(builder, "gravity.enabled", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live);
            AddParameter(builder, "drag", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live);
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
                new[] { Read("spawn.key", FluxParticleReadVersion.Provided) }, new[] { Init("particle.velocity"), Init("particle.spawn-key") }, null,
                Hlsl + "SpawnVelocity.hlsl", "FluxFX_SpawnVelocity", new[] { "spawn.velocity" }));
            builder.AddOperation(new FluxParticleOperationDefinition("gravity", Core, FluxParticlePhase.Update,
                new[] { Read("particle.velocity", FluxParticleReadVersion.StepPrevious), Read("step.delta-time", FluxParticleReadVersion.Provided) },
                new[] { Modify("particle.velocity") }, null, Hlsl + "Gravity.hlsl", "FluxFX_Gravity", new[] { "gravity" }, "gravity.enabled"));
            builder.AddOperation(new FluxParticleOperationDefinition("drag", Core, FluxParticlePhase.Update,
                new[] { Read("particle.velocity", FluxParticleReadVersion.AfterOperation, "gravity"), Read("step.delta-time", FluxParticleReadVersion.Provided) },
                new[] { Modify("particle.velocity") }, new[] { "gravity" }, Hlsl + "Drag.hlsl", "FluxFX_Drag", new[] { "drag" }));
            builder.AddOperation(new FluxParticleOperationDefinition("motion", Core, FluxParticlePhase.Update,
                new[] { Read("particle.velocity", FluxParticleReadVersion.AfterOperation, "drag"),
                    Read("particle.position", FluxParticleReadVersion.StepPrevious), Read("particle.age", FluxParticleReadVersion.StepPrevious),
                    Read("step.delta-time", FluxParticleReadVersion.Provided) },
                new[] { Modify("particle.position"), Modify("particle.age") }, new[] { "drag" }, Hlsl + "Motion.hlsl", "FluxFX_Motion"));
            builder.AddOperation(new FluxParticleOperationDefinition("render.start", Core, FluxParticlePhase.Render,
                new[] { Read("particle.start-color", FluxParticleReadVersion.Provided) },
                new[] { new FluxParticleValueWrite("render.color", FluxParticleWriteKind.Replace) }, null,
                Hlsl + "RenderStart.hlsl", "FluxFX_RenderStart"));
            builder.AddOperation(new FluxParticleOperationDefinition("render.color", Core, FluxParticlePhase.Render,
                new[] { Read("render.color", FluxParticleReadVersion.AfterOperation, "render.start"),
                    Read("particle.age", FluxParticleReadVersion.Committed), Read("particle.lifetime", FluxParticleReadVersion.Provided) },
                new[] { Modify("render.color") }, new[] { "render.start" }, Hlsl + "RenderColor.hlsl", "FluxFX_RenderColor", new[] { "color.end" }));
            return builder;
        }

        /// <summary>

        /// Ageが負値なら非生存とする基本Lifecycleを返します。

        /// </summary>
        /// <returns>
        /// 基本Programの生存判定。
        /// </returns>
        public static FluxParticleShaderLifecycle CreateLifecycle() => new FluxParticleShaderLifecycle("particle.age", "particle.lifetime");

        static void AddValue(FluxParticleDefinitionBuilder builder, string id, FluxParticleValueType type, FluxParticleWritePolicy policy)
        {
            builder.AddValue(new FluxParticleValueDefinition(id, type, policy, Core, type == FluxParticleValueType.UInt ? 1048575u : (uint?)null));
        }

        static void AddParameter(FluxParticleDefinitionBuilder builder, string id, FluxParticleValueType type, FluxParticleParameterPolicy policy)
        {
            builder.AddParameter(new FluxParticleParameterDefinition(id, type, policy));
        }

        static FluxParticleValueRead Read(string id, FluxParticleReadVersion version, string producer = null) => new FluxParticleValueRead(id, version, producer);
        static FluxParticleValueWrite Init(string id) => new FluxParticleValueWrite(id, FluxParticleWriteKind.Initialize);
        static FluxParticleValueWrite Modify(string id) => new FluxParticleValueWrite(id, FluxParticleWriteKind.Modify);
    }
}
