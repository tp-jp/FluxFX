using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Scripts;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// Authoring設定とModuleからParticleの論理定義を構築します。
    /// </summary>
    public static class FluxParticleAuthoringDefinitionBuilder
    {
        /// <summary>
        /// CoreとModuleの論理定義およびParameter初期値を構築します。
        /// </summary>
        /// <param name="authoring">Particleの設定。</param>
        /// <param name="initialValues">Parameter IDに対応する初期値。</param>
        /// <returns>構築した論理定義。</returns>
        public static FluxParticleDefinitionBuilder Create(FluxParticleAuthoring authoring,
            out IReadOnlyDictionary<string, Vector4> initialValues)
        {
            var source = FluxParticleCoreDefinitions.Create();
            var definitions = new FluxParticleDefinitionBuilder();
            foreach (var value in source.Values.Values) definitions.AddValue(value);
            if (authoring.SimulationSpace == FluxParticleSimulationSpace.World)
            {
                for (var row = 0; row < 3; row++)
                {
                    var id = "emitter.row" + row;
                    definitions.AddValue(new FluxParticleValueDefinition(id, FluxParticleValueType.Float4,
                        FluxParticleWritePolicy.ReadOnly, "fluxfx.core"));
                    definitions.AddProvider(new FluxParticleValueProvider(id, FluxParticleProviderKind.Uniform));
                }
            }
            var moduleContext = new FluxParticleProgramContribution(definitions, authoring.Render.Mode);
            new FluxParticleModuleRegistry().Contribute(authoring.Modules, moduleContext);
            var hasRotation = authoring.Render.StartRotation != Vector3.zero || definitions.Operations.Values.Any(operation => operation.Writes.Any(write =>
                write.ValueId == "render.rotation"));
            if (hasRotation)
            {
                definitions.AddValue(new FluxParticleValueDefinition("render.rotation", FluxParticleValueType.Float3,
                    FluxParticleWritePolicy.SharedMutable, "fluxfx.core"));
            }
            AddProviders(authoring, source, definitions);
            AddOperationsAndParameters(authoring, source, definitions, hasRotation);
            initialValues = new ReadOnlyDictionary<string, Vector4>(
                FluxParticleAuthoringInitialValues.Create(authoring, moduleContext.InitialValues, hasRotation));
            return definitions;
        }

        /// <summary>
        /// Authoring設定に応じたCore Providerを登録します。
        /// </summary>
        /// <param name="authoring">Particleの設定。</param>
        /// <param name="source">Coreの基本定義。</param>
        /// <param name="definitions">Providerの登録先。</param>
        static void AddProviders(FluxParticleAuthoring authoring, FluxParticleDefinitionBuilder source,
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

        /// <summary>
        /// Authoring設定に応じたParameterとOperationを登録します。
        /// </summary>
        /// <param name="authoring">Particleの設定。</param>
        /// <param name="source">Coreの基本定義。</param>
        /// <param name="definitions">定義の登録先。</param>
        /// <param name="hasRotation">Rotationを使用するか。</param>
        static void AddOperationsAndParameters(FluxParticleAuthoring authoring, FluxParticleDefinitionBuilder source,
            FluxParticleDefinitionBuilder definitions, bool hasRotation)
        {
            var randomColor = authoring.Render.StartColorMode == FluxParticleStartColorMode.RandomBetweenTwoConstants;
            var randomSize = authoring.Render.StartSizeMode == FluxParticleStartSizeMode.RandomBetweenTwoConstants;
            foreach (var parameter in source.Parameters.Values.Where(parameter => parameter.Id != "spawn.velocity" &&
                !(randomColor && parameter.Id == "color.start"))) definitions.AddParameter(parameter);
            if (randomColor)
            {
                definitions.AddParameter(new FluxParticleParameterDefinition("color.start-min", FluxParticleValueType.Float4, FluxParticleParameterPolicy.Static));
                definitions.AddParameter(new FluxParticleParameterDefinition("color.start-max", FluxParticleValueType.Float4, FluxParticleParameterPolicy.Static));
            }
            var radialShape = authoring.Shape.Type == FluxParticleShapeType.Sphere ||
                authoring.Shape.Type == FluxParticleShapeType.Hemisphere || authoring.Shape.Type == FluxParticleShapeType.Cone;
            if (radialShape)
            {
                definitions.AddValue(new FluxParticleValueDefinition("spawn.direction", FluxParticleValueType.Float3,
                    FluxParticleWritePolicy.ReadOnly, "fluxfx.core"));
                var shapeDirectionHlsl = authoring.Shape.Type == FluxParticleShapeType.Sphere
                    ? "AuthoringSphereSpawnDirection.hlsl" :
                    authoring.Shape.Type == FluxParticleShapeType.Hemisphere
                        ? "AuthoringHemisphereSpawnDirection.hlsl" : "AuthoringConeSpawnDirection.hlsl";
                definitions.AddProvider(new FluxParticleValueProvider("spawn.direction", FluxParticleProviderKind.Derived,
                    new[] { "spawn.key" },
                    authoring.Shape.Type == FluxParticleShapeType.Cone ? new[] { "shape.angle" } : Array.Empty<string>(),
                    "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/" + shapeDirectionHlsl,
                    "FluxFX_SpawnDirection"));
            }
            if (authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants)
            {
                definitions.AddParameter(new FluxParticleParameterDefinition("spawn.speed-range", FluxParticleValueType.Float2, FluxParticleParameterPolicy.Live));
            }
            else
            {
                definitions.AddParameter(new FluxParticleParameterDefinition("spawn.speed", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Sphere || authoring.Shape.Type == FluxParticleShapeType.Hemisphere ||
                authoring.Shape.Type == FluxParticleShapeType.Circle || authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                definitions.AddParameter(new FluxParticleParameterDefinition("shape.radius", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                definitions.AddParameter(new FluxParticleParameterDefinition("shape.angle", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Box)
            {
                definitions.AddParameter(new FluxParticleParameterDefinition("shape.size", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Live));
            }
            definitions.AddProvider(new FluxParticleValueProvider("particle.start-size", FluxParticleProviderKind.Derived,
                randomSize ? new[] { "particle.spawn-key" } : Array.Empty<string>(),
                new[] { randomSize ? "render.size-range" : "render.size" },
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/" + (randomSize ? "RandomStartSize.hlsl" : "StartSize.hlsl"),
                "FluxFX_StartSize"));
            AddOperation(authoring, definitions, new FluxParticleOperationDefinition("render.size-start", "fluxfx.core", FluxParticlePhase.Render,
                new[] { new FluxParticleValueRead("particle.start-size", FluxParticleReadVersion.Provided) },
                new[] { new FluxParticleValueWrite("render.size", FluxParticleWriteKind.Replace) }, null,
                "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/RenderStartSize.hlsl", "FluxFX_RenderStartSize"));
            definitions.AddParameter(new FluxParticleParameterDefinition(randomSize ? "render.size-range" : "render.size",
                randomSize ? FluxParticleValueType.Float2 : FluxParticleValueType.Float, FluxParticleParameterPolicy.Static));
            if (hasRotation)
            {
                definitions.AddParameter(new FluxParticleParameterDefinition("rotation.start", FluxParticleValueType.Float3, FluxParticleParameterPolicy.Static));
            }
            foreach (var operation in source.Operations.Values)
            {
                if (operation.Id == "spawn.position" && authoring.Shape.Type != FluxParticleShapeType.Point)
                {
                    var shapeHlsl = authoring.Shape.Type == FluxParticleShapeType.Sphere ? "AuthoringSphereSpawnPosition.hlsl" :
                        authoring.Shape.Type == FluxParticleShapeType.Hemisphere ? "AuthoringHemisphereSpawnPosition.hlsl" :
                        authoring.Shape.Type == FluxParticleShapeType.Circle ? "AuthoringCircleSpawnPosition.hlsl" :
                        authoring.Shape.Type == FluxParticleShapeType.Cone ? "AuthoringConeSpawnPosition.hlsl" :
                        "AuthoringBoxSpawnPosition.hlsl";
                    var shapeParameter = authoring.Shape.Type == FluxParticleShapeType.Box ? "shape.size" : "shape.radius";
                    AddOperation(authoring, definitions, new FluxParticleOperationDefinition(operation.Id, operation.OwnerId, operation.Phase,
                        new[] { new FluxParticleValueRead("spawn.key", FluxParticleReadVersion.Provided) },
                        operation.Writes, operation.Dependencies,
                        "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/" + shapeHlsl, operation.EntryPoint,
                        new[] { "spawn.position", shapeParameter }));
                }
                else if (operation.Id == "spawn.velocity")
                {
                    AddOperation(authoring, definitions, new FluxParticleOperationDefinition(operation.Id, operation.OwnerId, operation.Phase,
                        radialShape ? operation.Reads.Concat(new[] {
                            new FluxParticleValueRead("spawn.direction", FluxParticleReadVersion.Provided) }) : operation.Reads,
                        operation.Writes, operation.Dependencies,
                        radialShape
                            ? (authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants
                                ? "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringRadialRandomSpawnVelocity.hlsl"
                                : "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringRadialSpawnVelocity.hlsl")
                            : (authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants
                                ? "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringRandomSpawnVelocity.hlsl"
                                : "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringSpawnVelocity.hlsl"),
                        operation.EntryPoint,
                        new[] { authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants
                            ? "spawn.speed-range" : "spawn.speed" }));
                }
                else
                {
                    AddOperation(authoring, definitions, operation);
                }
            }
            if (hasRotation)
            {
                AddOperation(authoring, definitions, new FluxParticleOperationDefinition("render.rotation-start", "fluxfx.core", FluxParticlePhase.Render,
                    Array.Empty<FluxParticleValueRead>(),
                    new[] { new FluxParticleValueWrite("render.rotation", FluxParticleWriteKind.Replace) }, null,
                    "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringStartRotation.hlsl", "FluxFX_RenderRotationStart",
                    new[] { "rotation.start" }));
            }
        }

        static void AddOperation(FluxParticleAuthoring authoring, FluxParticleDefinitionBuilder definitions,
            FluxParticleOperationDefinition operation)
        {
            if (authoring.SimulationSpace != FluxParticleSimulationSpace.World || operation.Phase != FluxParticlePhase.Spawn)
            {
                definitions.AddOperation(operation);
                return;
            }
            var reads = operation.Reads.Concat(Enumerable.Range(0, 3).Select(row =>
                new FluxParticleValueRead("emitter.row" + row, FluxParticleReadVersion.Provided)));
            definitions.AddOperation(new FluxParticleOperationDefinition(operation.Id, operation.OwnerId, operation.Phase,
                reads, operation.Writes, operation.Dependencies, operation.HlslPath.Replace(".hlsl", "World.hlsl"),
                operation.EntryPoint, operation.Parameters, operation.EnabledParameterId));
        }
    }
}
