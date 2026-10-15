using System;
using System.Linq;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharpEditor;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// Main・Emission・ShapeのRuntime参照とビルド済みBindingを設定します。
    /// </summary>
    internal static class FluxParticleRuntimeApiBuilder
    {
        public static void Configure(FluxParticleExecutor executor, FluxParticleAuthoring authoring, FluxParticleParameterDefinition[] parameters)
        {
            var system = executor.GetComponentInParent<FluxParticleSystem>();
            if (system == null) return;
            var emitter = system.GetComponentInChildren<FluxParticleEmitter>(true);
            if (emitter == null)
            {
                throw new InvalidOperationException("Runtime API requires FluxParticleEmitter.");
            }
            var main = new int[FluxParticleMainHandle.ParameterCount];
            var shape = new int[FluxParticleShapeHandle.ParameterCount];
            main[FluxParticleMainHandle.StartLifetimeRangeParameterIndex] = Resolve(parameters, "lifetime.range", FluxParticleValueType.Float2);
            main[FluxParticleMainHandle.StartSpeedParameterIndex] = Resolve(parameters, "spawn.speed", FluxParticleValueType.Float);
            main[FluxParticleMainHandle.StartSpeedRangeParameterIndex] = Resolve(parameters, "spawn.speed-range", FluxParticleValueType.Float2);
            main[FluxParticleMainHandle.StartColorParameterIndex] = Resolve(parameters, "color.start", FluxParticleValueType.Float4);
            main[FluxParticleMainHandle.StartColorMinParameterIndex] = Resolve(parameters, "color.start-min", FluxParticleValueType.Float4);
            main[FluxParticleMainHandle.StartColorMaxParameterIndex] = Resolve(parameters, "color.start-max", FluxParticleValueType.Float4);
            main[FluxParticleMainHandle.StartSizeParameterIndex] = Resolve(parameters, "render.size", FluxParticleValueType.Float);
            main[FluxParticleMainHandle.StartSizeRangeParameterIndex] = Resolve(parameters, "render.size-range", FluxParticleValueType.Float2);
            main[FluxParticleMainHandle.StartRotationParameterIndex] = Resolve(parameters, "rotation.start", FluxParticleValueType.Float3);
            main[FluxParticleMainHandle.StartRotationMinParameterIndex] = Resolve(parameters, "rotation.start-min", FluxParticleValueType.Float3);
            main[FluxParticleMainHandle.StartRotationMaxParameterIndex] = Resolve(parameters, "rotation.start-max", FluxParticleValueType.Float3);
            shape[FluxParticleShapeHandle.PositionParameterIndex] = Resolve(parameters, "spawn.position", FluxParticleValueType.Float3);
            shape[FluxParticleShapeHandle.RadiusParameterIndex] = Resolve(parameters, "shape.radius", FluxParticleValueType.Float);
            shape[FluxParticleShapeHandle.AngleParameterIndex] = Resolve(parameters, "shape.angle", FluxParticleValueType.Float);
            shape[FluxParticleShapeHandle.SizeParameterIndex] = Resolve(parameters, "shape.size", FluxParticleValueType.Float3);
            system.SetProgramVariable("particleExecutor", executor);
            system.SetProgramVariable("particleEmitter", emitter);
            system.SetProgramVariable("mainParameterSlots", main);
            system.SetProgramVariable("shapeParameterSlots", shape);
            system.SetProgramVariable("simulationSpace", authoring.SimulationSpace);
            system.SetProgramVariable("shapeType", authoring.Shape.Type);
            system.SetProgramVariable("renderMode", authoring.Render.Mode);
            system.SetProgramVariable("_main", null);
            system.SetProgramVariable("_shape", null);
            system.SetProgramVariable("_emission", null);

            var bursts = authoring.Emission.Bursts.OrderBy(burst => burst.Time).ToArray();
            emitter.SetProgramVariable("rate", authoring.Emission.Rate);
            emitter.SetProgramVariable("startDelay", authoring.Playback.StartDelay);
            emitter.SetProgramVariable("duration", authoring.Playback.Duration);
            emitter.SetProgramVariable("loop", authoring.Playback.Loop);
            emitter.SetProgramVariable("burstTimes", bursts.Select(burst => burst.Time).ToArray());
            emitter.SetProgramVariable("burstCounts", bursts.Select(burst => burst.Count).ToArray());
            EditorUtility.SetDirty(emitter);
            EditorUtility.SetDirty(system);
        }

        static int Resolve(FluxParticleParameterDefinition[] parameters, string id, FluxParticleValueType type)
        {
            var index = Array.FindIndex(parameters, parameter => parameter.Id == id);
            if (index >= 0 && parameters[index].Type != type)
            {
                throw new InvalidOperationException($"Runtime API requires Parameter '{id}' with type {type}.");
            }
            return index;
        }
    }
}

