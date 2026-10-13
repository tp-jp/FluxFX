using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.FX.Udon;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Compilation
{
    /// <summary>
    /// 既存Authoringの基本構成を新Compilerの宣言とParameter初期値へ変換します。
    /// 未移行の設定は無視せず、コンパイル前に検出します。
    /// </summary>
    public sealed class FluxParticleAuthoringProgram
    {
        /// <summary>
        /// State LayoutとShaderを含むコンパイル済みProgramです。
        /// </summary>
        public FluxParticleShaderProgram Program { get; }

        /// <summary>
        /// 論理Parameter IDに対応する初期値です。Floatはx成分を使用します。
        /// </summary>
        public IReadOnlyDictionary<string, Vector4> InitialValues { get; }

        /// <summary>
        /// Authoringを検証し、対応するModuleだけから基本Programを構築します。
        /// </summary>
        /// <param name="authoring">変換対象。</param>
        public FluxParticleAuthoringProgram(FluxParticleAuthoring authoring)
        {
            if (authoring == null)
            {
                throw new ArgumentNullException(nameof(authoring));
            }
            Require(authoring.SimulationSpace == FluxParticleSimulationSpace.Local, "Local simulation space");
            Require(authoring.Shape.Type == FluxParticleShapeType.Point || authoring.Shape.Type == FluxParticleShapeType.Sphere ||
                authoring.Shape.Type == FluxParticleShapeType.Hemisphere || authoring.Shape.Type == FluxParticleShapeType.Circle ||
                authoring.Shape.Type == FluxParticleShapeType.Box || authoring.Shape.Type == FluxParticleShapeType.Cone, "Supported shape");
            if (authoring.Shape.Type == FluxParticleShapeType.Sphere || authoring.Shape.Type == FluxParticleShapeType.Hemisphere ||
                authoring.Shape.Type == FluxParticleShapeType.Circle || authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                Require(FiniteNonNegative(authoring.Shape.Radius), "Non-negative shape radius");
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                Require(FiniteNonNegative(authoring.Shape.Angle) && authoring.Shape.Angle < 90f, "Cone angle in [0, 90) degrees");
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Box)
            {
                var size = authoring.Shape.Size;
                Require(FiniteNonNegative(size.x) && FiniteNonNegative(size.y) && FiniteNonNegative(size.z), "Non-negative Box size");
            }
            Require(authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.Constant ||
                authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants, "Supported start speed mode");
            if (authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants)
            {
                Require(FiniteNonNegative(authoring.InitialVelocity.Min) && FiniteNonNegative(authoring.InitialVelocity.Max),
                    "Finite non-negative start speed range");
            }
            Require(authoring.Render.Mode == FluxParticleRenderMode.Billboard, "Billboard rendering");
            Require(authoring.Render.StartColorMode == FluxParticleStartColorMode.Constant, "Constant start color");
            Require(authoring.Render.StartSizeMode == FluxParticleStartSizeMode.Constant, "Constant start size");
            Require(authoring.Render.StartRotation == Vector3.zero, "Zero start rotation");
            Require(authoring.Render.Material == null || authoring.Render.Material.shader.name == "FluxFX/ParticleRender", "FluxFX/ParticleRender material or no material");
            Require(FiniteNonNegative(authoring.Emission.Rate) && FiniteNonNegative(authoring.Playback.StartDelay) &&
                FiniteNonNegative(authoring.Playback.Duration) && authoring.Playback.Duration > 0, "Finite playback and emission settings");
            foreach (var burst in authoring.Emission.Bursts)
            {
                Require(burst != null && FiniteNonNegative(burst.Time) && burst.Count > 0, "Valid emission bursts");
            }
            var seen = new HashSet<Type>();
            FluxParticleGravityModule gravity = null;
            FluxParticleDragModule drag = null;
            FluxParticleColorOverLifetimeModule color = null;
            foreach (var module in authoring.Modules)
            {
                if (module == null)
                {
                    throw new InvalidOperationException("Program Backend: missing module definition.");
                }
                if (!seen.Add(module.GetType()))
                {
                    throw new InvalidOperationException($"Program Backend: duplicate module {module.GetType().Name}.");
                }
                if (module is FluxParticleGravityModule g)
                {
                    gravity = g;
                }
                else if (module is FluxParticleDragModule d)
                {
                    drag = d;
                }
                else if (module is FluxParticleColorOverLifetimeModule c)
                {
                    Require(c.Mode == FluxParticleColorOverLifetimeMode.Linear, "Linear color over lifetime");
                    color = c;
                }
                else
                {
                    throw new InvalidOperationException($"Program Backend: module is not migrated: {module.GetType().Name}.");
                }
            }
            var source = FluxParticleBasicProgram.CreateDefinitions();
            var definitions = new FluxParticleDefinitionBuilder();
            foreach (var value in source.Values.Values) definitions.AddValue(value);
            foreach (var provider in source.Providers.Values)
            {
                definitions.AddProvider(provider.ValueId == "particle.lifetime"
                    ? new FluxParticleValueProvider(provider.ValueId, provider.Kind, provider.Inputs, provider.Parameters,
                        "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/AuthoringLifetime.hlsl", provider.EntryPoint)
                    : provider);
            }
            foreach (var parameter in source.Parameters.Values.Where(parameter => parameter.Id != "spawn.velocity")) definitions.AddParameter(parameter);
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
            definitions.AddParameter(new FluxParticleParameterDefinition("drag.enabled", FluxParticleValueType.Float, FluxParticleParameterPolicy.Live));
            definitions.AddParameter(new FluxParticleParameterDefinition("render.size", FluxParticleValueType.Float, FluxParticleParameterPolicy.Static));
            foreach (var operation in source.Operations.Values)
            {
                if (operation.Id == "render.color" && (color == null || !color.Enabled))
                {
                    continue;
                }
                if (operation.Id == "spawn.position" && authoring.Shape.Type != FluxParticleShapeType.Point)
                {
                    var shapeHlsl = authoring.Shape.Type == FluxParticleShapeType.Sphere ? "AuthoringSphereSpawnPosition.hlsl" :
                        authoring.Shape.Type == FluxParticleShapeType.Hemisphere ? "AuthoringHemisphereSpawnPosition.hlsl" :
                        authoring.Shape.Type == FluxParticleShapeType.Circle ? "AuthoringCircleSpawnPosition.hlsl" :
                        authoring.Shape.Type == FluxParticleShapeType.Cone ? "AuthoringConeSpawnPosition.hlsl" :
                        "AuthoringBoxSpawnPosition.hlsl";
                    var shapeParameter = authoring.Shape.Type == FluxParticleShapeType.Box ? "shape.size" : "shape.radius";
                    definitions.AddOperation(new FluxParticleOperationDefinition(operation.Id, operation.OwnerId, operation.Phase,
                        new[] { new FluxParticleValueRead("spawn.key", FluxParticleReadVersion.Provided) },
                        operation.Writes, operation.Dependencies,
                        "Packages/com.tplab.flux.fx/Editor/Compilation/Hlsl/" + shapeHlsl, operation.EntryPoint,
                        new[] { "spawn.position", shapeParameter }));
                }
                else if (operation.Id == "spawn.velocity")
                {
                    definitions.AddOperation(new FluxParticleOperationDefinition(operation.Id, operation.OwnerId, operation.Phase,
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
                else if (operation.Id == "drag")
                {
                    definitions.AddOperation(new FluxParticleOperationDefinition(operation.Id, operation.OwnerId, operation.Phase,
                        operation.Reads, operation.Writes, operation.Dependencies, operation.HlslPath, operation.EntryPoint,
                        operation.Parameters.Concat(new[] { "drag.enabled" }), "drag.enabled"));
                }
                else
                {
                    definitions.AddOperation(operation);
                }
            }
            var life = authoring.Lifetime;
            var min = life.Mode == FluxParticleStartLifetimeMode.Constant ? life.Lifetime : Mathf.Min(life.Min, life.Max);
            var max = life.Mode == FluxParticleStartLifetimeMode.Constant ? life.Lifetime : Mathf.Max(life.Min, life.Max);
            var values = new Dictionary<string, Vector4>(StringComparer.Ordinal)
            {
                ["spawn.position"] = Vector4.zero,
                ["gravity"] = gravity == null ? Vector4.zero : (Vector4)gravity.Gravity,
                ["gravity.enabled"] = new Vector4(gravity != null && gravity.Enabled ? 1 : 0, 0, 0, 0),
                ["drag"] = new Vector4(drag == null ? 0 : drag.Drag, 0, 0, 0),
                ["drag.enabled"] = new Vector4(drag != null && drag.Enabled ? 1 : 0, 0, 0, 0),
                ["lifetime.range"] = new Vector4(min, max, 0, 0),
                ["color.start"] = authoring.Render.StartColor,
                ["color.end"] = color == null ? authoring.Render.StartColor : color.EndColor,
                ["render.size"] = new Vector4(authoring.Render.StartSize, 0, 0, 0)
            };
            if (authoring.InitialVelocity.Mode == FluxParticleStartSpeedMode.RandomBetweenTwoConstants)
            {
                values.Add("spawn.speed-range", new Vector4(Mathf.Min(authoring.InitialVelocity.Min, authoring.InitialVelocity.Max),
                    Mathf.Max(authoring.InitialVelocity.Min, authoring.InitialVelocity.Max), 0, 0));
            }
            else
            {
                values.Add("spawn.speed", new Vector4(authoring.InitialVelocity.Speed, 0, 0, 0));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Sphere || authoring.Shape.Type == FluxParticleShapeType.Hemisphere ||
                authoring.Shape.Type == FluxParticleShapeType.Circle || authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                values.Add("shape.radius", new Vector4(authoring.Shape.Radius, 0, 0, 0));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Cone)
            {
                values.Add("shape.angle", new Vector4(authoring.Shape.Angle, 0, 0, 0));
            }
            if (authoring.Shape.Type == FluxParticleShapeType.Box)
            {
                values.Add("shape.size", authoring.Shape.Size);
            }
            foreach (var value in values)
            {
                for (var c = 0; c < 4; c++)
                {
                    if (float.IsNaN(value.Value[c]) || float.IsInfinity(value.Value[c]))
                    {
                        throw new InvalidOperationException($"Non-finite Parameter: {value.Key}");
                    }
                }
            }
            Require(min >= 0 && authoring.Render.StartSize >= 0, "Non-negative lifetime and size");
            Program = new FluxParticleProgramShaderGenerator().Generate(definitions, "Hidden/FluxFX/Program", FluxParticleBasicProgram.CreateLifecycle());
            InitialValues = new System.Collections.ObjectModel.ReadOnlyDictionary<string, Vector4>(values);
        }

        /// <summary>
        /// Render Operationの出力とState Layoutを使い、Billboard用Shaderを生成します。
        /// </summary>
        /// <returns>UnityへImportするShaderLabソース。</returns>
        public string GenerateRenderer()
        {
            if (!Program.Program.StateLayout.TryGetBinding("particle.position", out var position))
            {
                throw new InvalidOperationException("Position State is required for Billboard rendering.");
            }
            var sampler = FluxParticleHlslNames.PageProperty(position.PageId, FluxParticlePageVersion.Updated);
            var channels = "rgba".Substring(position.ChannelOffset, position.ComponentCount);
            // RenderSource only declares Pages that its Operations read. Position may need a separate sampler.
            var declaration = "sampler2D " + sampler + ";";
            var extra = Program.RenderSource.Contains(declaration) ? "" : declaration + "\n";
            var pageProperties = string.Join("\n", Program.Program.StateLayout.Pages.Select(page =>
                $"[HideInInspector] {FluxParticleHlslNames.PageProperty(page.Id, FluxParticlePageVersion.Updated)} (\"State Page {page.Id}\", 2D) = \"black\" {{}}"));
            return FluxParticleProgramShaderGenerator.ExpandTemplate("Billboard", new Dictionary<string, string>
            {
                ["PageProperties"] = pageProperties,
                ["RenderCode"] = extra + Program.RenderSource,
                ["Position"] = $"tex2Dlod({sampler}, float4(stateUV, 0.0, 0.0)).{channels}",
                ["Color"] = "render." + FluxParticleHlslNames.Value("render.color"),
                ["Size"] = FluxParticleHlslNames.ParameterProperty("render.size")
            });
        }

        static bool FiniteNonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;

        static void Require(bool valid, string requirement)
        {
            if (valid)
            {
                return;
            }
            throw new InvalidOperationException($"Program Backend currently requires: {requirement}.");
        }
    }
}
