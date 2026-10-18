using TpLab.Flux.FX.Editor.Modules.Blink;
using TpLab.Flux.FX.Editor.Modules.TextureSheetAnimation;
using TpLab.Flux.FX.Editor.Modules.RotationOverLifetime;
using TpLab.Flux.FX.Editor.Modules.SizeOverLifetime;
using TpLab.Flux.FX.Editor.Modules.ColorOverLifetime;
using TpLab.Flux.FX.Editor.Modules.VelocityOverLifetime;
using TpLab.Flux.FX.Editor.Modules.Vortex;
using TpLab.Flux.FX.Editor.Modules.Noise;
using TpLab.Flux.FX.Editor.Modules.LimitVelocity;
using TpLab.Flux.FX.Editor.Modules.Drag;
using TpLab.Flux.FX.Editor.Modules.Force;
using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Editor.Modules.Gravity;
using TpLab.Flux.FX.Editor.Modules.Trails;
using TpLab.Flux.FX.Scripts.Modules;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public enum FluxParticleLiveTarget
    {
        Executor,
        Emitter
    }

    /// <summary>
    /// Inspectorの項目とRuntime契約の対応
    /// </summary>
    public sealed class FluxParticleLiveBinding
    {
        /// <summary>
        /// 対象Moduleの型。Authoring直下の設定はnull
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        /// ModuleまたはAuthoringからの相対プロパティパス
        /// </summary>
        public string PropertyPath { get; }

        /// <summary>
        /// 反映先Parameterの契約
        /// </summary>
        public FluxParticleParameterContract Contract { get; }

        /// <summary>
        /// ベクトルの一成分を編集する場合の番号。それ以外は-1
        /// </summary>
        public int Component { get; }

        public FluxParticleLiveTarget Target { get; }

        public FluxParticleLiveBinding(Type moduleType, string propertyPath, FluxParticleParameterContract contract,
            int component = -1, FluxParticleLiveTarget target = FluxParticleLiveTarget.Executor)
        {
            ModuleType = moduleType;
            PropertyPath = propertyPath;
            Contract = contract;
            Component = component;
            Target = target;
        }
    }

    /// <summary>
    /// Live編集するInspector項目と契約の対応を提供します。
    /// </summary>
    public abstract class FluxParticleLiveInspectorProvider
    {
        /// <summary>
        /// Inspectorの項目とRuntime契約の対応
        /// </summary>
        public abstract IEnumerable<FluxParticleLiveBinding> Bindings { get; }
    }

    public sealed class FluxParticleStandardLiveInspectorProvider : FluxParticleLiveInspectorProvider
    {
        public override IEnumerable<FluxParticleLiveBinding> Bindings => new[]
        {
            new FluxParticleLiveBinding(typeof(FluxParticleGravityModule), "enabled", FluxParticleGravityContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleGravityModule), "gravity", FluxParticleGravityContract.Acceleration),
            new FluxParticleLiveBinding(typeof(FluxParticleTrailsModule), "enabled", FluxParticleTrailsContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleTrailsModule), "width", FluxParticleTrailsContract.Width),
            new FluxParticleLiveBinding(typeof(FluxParticleTrailsModule), "color", FluxParticleTrailsContract.Color),
            new FluxParticleLiveBinding(typeof(FluxParticleTrailsModule), "duration", FluxParticleTrailsContract.Duration),
            new FluxParticleLiveBinding(typeof(FluxParticleForceModule), "enabled", FluxParticleForceContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleForceModule), "force", FluxParticleForceContract.Force),
            new FluxParticleLiveBinding(typeof(FluxParticleDragModule), "enabled", FluxParticleDragContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleDragModule), "drag", FluxParticleDragContract.Drag),
            new FluxParticleLiveBinding(typeof(FluxParticleLimitVelocityModule), "enabled", FluxParticleLimitVelocityContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleLimitVelocityModule), "maxSpeed", FluxParticleLimitVelocityContract.MaxSpeed),
            new FluxParticleLiveBinding(typeof(FluxParticleNoiseModule), "enabled", FluxParticleNoiseContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleNoiseModule), "strength", FluxParticleNoiseContract.Strength),
            new FluxParticleLiveBinding(typeof(FluxParticleNoiseModule), "scale", FluxParticleNoiseContract.Scale),
            new FluxParticleLiveBinding(typeof(FluxParticleNoiseModule), "speed", FluxParticleNoiseContract.Speed),
            new FluxParticleLiveBinding(typeof(FluxParticleVortexModule), "enabled", FluxParticleVortexContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleVortexModule), "center", FluxParticleVortexContract.Center),
            new FluxParticleLiveBinding(typeof(FluxParticleVortexModule), "axis", FluxParticleVortexContract.Axis),
            new FluxParticleLiveBinding(typeof(FluxParticleVortexModule), "strength", FluxParticleVortexContract.Strength),
            new FluxParticleLiveBinding(typeof(FluxParticleVelocityOverLifetimeModule), "enabled", FluxParticleVelocityOverLifetimeContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleVelocityOverLifetimeModule), "start", FluxParticleVelocityOverLifetimeContract.Start),
            new FluxParticleLiveBinding(typeof(FluxParticleVelocityOverLifetimeModule), "end", FluxParticleVelocityOverLifetimeContract.End),
            new FluxParticleLiveBinding(typeof(FluxParticleVelocityOverLifetimeModule), "orbital", FluxParticleVelocityOverLifetimeContract.Orbital),
            new FluxParticleLiveBinding(typeof(FluxParticleVelocityOverLifetimeModule), "offset", FluxParticleVelocityOverLifetimeContract.Offset),
            new FluxParticleLiveBinding(typeof(FluxParticleVelocityOverLifetimeModule), "radial", FluxParticleVelocityOverLifetimeContract.Radial),
            new FluxParticleLiveBinding(typeof(FluxParticleColorOverLifetimeModule), "enabled", FluxParticleColorOverLifetimeContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleColorOverLifetimeModule), "endColor", FluxParticleColorOverLifetimeContract.EndColor),
            new FluxParticleLiveBinding(typeof(FluxParticleSizeOverLifetimeModule), "enabled", FluxParticleSizeOverLifetimeContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleSizeOverLifetimeModule), "endSize", FluxParticleSizeOverLifetimeContract.EndSize),
            new FluxParticleLiveBinding(typeof(FluxParticleRotationOverLifetimeModule), "enabled", FluxParticleRotationOverLifetimeContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleRotationOverLifetimeModule), "endRotation", FluxParticleRotationOverLifetimeContract.EndRotation),
            new FluxParticleLiveBinding(typeof(FluxParticleTextureSheetAnimationModule), "enabled", FluxParticleTextureSheetAnimationContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleTextureSheetAnimationModule), "cycles", FluxParticleTextureSheetAnimationContract.Cycles),
            new FluxParticleLiveBinding(typeof(FluxParticleBlinkModule), "enabled", FluxParticleBlinkContract.Module.Enabled),
            new FluxParticleLiveBinding(typeof(FluxParticleBlinkModule), "frequency", FluxParticleBlinkContract.Frequency),
            new FluxParticleLiveBinding(typeof(FluxParticleBlinkModule), "duty", FluxParticleBlinkContract.Duty),
            new FluxParticleLiveBinding(typeof(FluxParticleBlinkModule), "minimum", FluxParticleBlinkContract.Minimum),
            new FluxParticleLiveBinding(typeof(FluxParticleBlinkModule), "smoothness", FluxParticleBlinkContract.Smoothness),
            new FluxParticleLiveBinding(typeof(FluxParticleBlinkModule), "randomPhase", FluxParticleBlinkContract.RandomPhase),
            new FluxParticleLiveBinding(typeof(FluxParticleBlinkModule), "timeMode", FluxParticleBlinkContract.TimeMode),
            new FluxParticleLiveBinding(typeof(FluxParticleBlinkModule), "target", FluxParticleBlinkContract.Target),
            new FluxParticleLiveBinding(null, "render.intensity", FluxParticleRendererContract.Intensity)
        };
    }

    public sealed class FluxParticleMainShapeLiveInspectorProvider : FluxParticleLiveInspectorProvider
    {
        public override IEnumerable<FluxParticleLiveBinding> Bindings => new[]
        {
            new FluxParticleLiveBinding(null, "initialVelocity.speed", Contract("spawn.speed", Udon.FluxParticleValueType.Float)),
            new FluxParticleLiveBinding(null, "initialVelocity.min", Contract("spawn.speed-range", Udon.FluxParticleValueType.Float2), 0),
            new FluxParticleLiveBinding(null, "initialVelocity.max", Contract("spawn.speed-range", Udon.FluxParticleValueType.Float2), 1),
            new FluxParticleLiveBinding(null, "shape.radius", Contract("shape.radius", Udon.FluxParticleValueType.Float)),
            new FluxParticleLiveBinding(null, "shape.angle", Contract("shape.angle", Udon.FluxParticleValueType.Float)),
            new FluxParticleLiveBinding(null, "shape.size", Contract("shape.size", Udon.FluxParticleValueType.Float3)),
            new FluxParticleLiveBinding(null, "emission.rate",
                FluxParticleParameter.Float("emission.rate").ApiName("Rate").Policy(FluxParticleParameterPolicy.Live).Minimum(0).Build(),
                target: FluxParticleLiveTarget.Emitter)
        };

        static FluxParticleParameterContract Contract(string id, Udon.FluxParticleValueType type)
        {
            return Authoring.FluxParticleMainShapeContracts.Apply(
                new Compilation.Definitions.FluxParticleParameterDefinition(id, type, Compilation.Definitions.FluxParticleParameterPolicy.Live),
                Udon.FluxParticleRenderMode.Billboard).Contract;
        }
    }
}
