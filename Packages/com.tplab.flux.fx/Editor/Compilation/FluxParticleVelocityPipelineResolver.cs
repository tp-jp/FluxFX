using System;
using System.Collections.Generic;
using System.Linq;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Velocityの更新列とMotionの読み取り元を解決します。
    /// Module固有の型を参照せず、Valueの書き込みを基準に接続します。
    /// </summary>
    public static class FluxParticleVelocityPipelineResolver
    {
        const string Velocity = "particle.velocity";
        const string Core = "fluxfx.core";

        /// <summary>
        /// 登録済みVelocity更新Operationを直列化し、Motionを最終値へ接続します。
        /// </summary>
        /// <param name="definitions">Moduleを登録済みの定義。</param>
        public static void Resolve(FluxParticleDefinitionBuilder definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            var finalWriter = FluxParticleValuePipelineResolver.Resolve(definitions, Velocity, definitions.OptionalDependencies);
            if (finalWriter == null)
            {
                // Spawn直後もVelocityのUpdated Pageを作り、Motionから最新値を読めるようにします。
                var identity = new FluxParticleOperationDefinition("velocity.identity", Core, FluxParticlePhase.Update,
                    new[] { new FluxParticleValueRead(Velocity, FluxParticleReadVersion.StepPrevious) },
                    new[] { new FluxParticleValueWrite(Velocity, FluxParticleWriteKind.Modify) }, null,
                    "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/VelocityIdentity.hlsl", "FluxFX_VelocityIdentity");
                definitions.AddOperation(identity);
                finalWriter = identity.Id;
            }

            var motion = new FluxParticleOperationDefinition("motion", Core, FluxParticlePhase.Update,
                new[]
                {
                    new FluxParticleValueRead(Velocity, FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.position", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("particle.age", FluxParticleReadVersion.StepPrevious),
                    new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
                },
                new[] { new FluxParticleValueWrite("particle.position", FluxParticleWriteKind.Modify),
                    new FluxParticleValueWrite("particle.age", FluxParticleWriteKind.Modify) }, null,
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/Motion.hlsl", "FluxFX_Motion");
            definitions.AddOperation(FluxParticleValuePipelineResolver.Connect(motion, Velocity, finalWriter));
        }
    }
}
