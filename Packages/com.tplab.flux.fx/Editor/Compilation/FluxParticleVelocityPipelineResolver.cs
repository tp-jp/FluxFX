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

            var writers = definitions.Operations.Values
                .Where(op => op.Phase == FluxParticlePhase.Update &&
                    op.Writes.Any(write => write.ValueId == Velocity)).ToArray();
            if (writers.Length > 1)
            {
                if (writers.Any(op => !op.VelocityOrder.HasValue))
                {
                    throw new InvalidOperationException("Velocity writers require explicit VelocityOrder when multiple modules are active.");
                }
                var duplicate = writers.GroupBy(op => op.VelocityOrder.Value).FirstOrDefault(group => group.Count() > 1);
                if (duplicate != null)
                {
                    throw new InvalidOperationException($"Conflicting VelocityOrder: {duplicate.Key}.");
                }
                writers = writers.OrderBy(op => op.VelocityOrder.Value).ToArray();
            }
            if (writers.Length == 0)
            {
                // Spawn直後もVelocityのUpdated Pageを作り、Motionから最新値を読めるようにします。
                var identity = new FluxParticleOperationDefinition("velocity.identity", Core, FluxParticlePhase.Update,
                    new[] { new FluxParticleValueRead(Velocity, FluxParticleReadVersion.StepPrevious) },
                    new[] { new FluxParticleValueWrite(Velocity, FluxParticleWriteKind.Modify) }, null,
                    "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/VelocityIdentity.hlsl", "FluxFX_VelocityIdentity");
                definitions.AddOperation(identity);
                writers = new[] { identity };
            }

            string previous = null;
            foreach (var writer in writers)
            {
                var reads = writer.Reads.Select(read => read.ValueId == Velocity
                    ? new FluxParticleValueRead(Velocity, previous == null ? FluxParticleReadVersion.StepPrevious :
                        FluxParticleReadVersion.AfterOperation, previous)
                    : read).ToArray();
                var dependencies = writer.Dependencies.Where(id => id != previous).ToList();
                if (previous != null) dependencies.Add(previous);
                definitions.ReplaceOperation(new FluxParticleOperationDefinition(writer.Id, writer.OwnerId,
                    writer.Phase, reads, writer.Writes, dependencies, writer.HlslPath, writer.EntryPoint,
                    writer.Parameters, writer.EnabledParameterId, writer.VelocityOrder));
                previous = writer.Id;
            }

            var motionReads = new[]
            {
                new FluxParticleValueRead(Velocity, previous == null ? FluxParticleReadVersion.StepPrevious :
                    FluxParticleReadVersion.AfterOperation, previous),
                new FluxParticleValueRead("particle.position", FluxParticleReadVersion.StepPrevious),
                new FluxParticleValueRead("particle.age", FluxParticleReadVersion.StepPrevious),
                new FluxParticleValueRead("step.delta-time", FluxParticleReadVersion.Provided)
            };
            definitions.AddOperation(new FluxParticleOperationDefinition("motion", Core, FluxParticlePhase.Update,
                motionReads, new[] { new FluxParticleValueWrite("particle.position", FluxParticleWriteKind.Modify),
                    new FluxParticleValueWrite("particle.age", FluxParticleWriteKind.Modify) },
                previous == null ? Array.Empty<string>() : new[] { previous },
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/Motion.hlsl", "FluxFX_Motion"));
        }
    }
}
