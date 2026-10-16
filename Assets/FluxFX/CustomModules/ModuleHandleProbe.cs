using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.CustomModules
{
    /// <summary>
    /// ModuleハンドルをUdon VMから操作し、結果を検証します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ModuleHandleProbe : UdonSharpBehaviour
    {
        public FluxParticleSystem target;
        public FluxParticleSystem empty;
        public bool passed;
        public int resultPage;
        public RenderTexture resultTexture;
        public int aliveStage;
        public int aliveResult;
        public bool alivePending;
        public bool aliveRequestAccepted;

        public RenderTexture trailOldPoint;

        public void VerifyTrails()
        {
            passed = false;
            target.Pause();
            var trails = target.Modules.Trails;
            if (trails == null || trails != target.Modules.Trails || empty.Modules.Trails != null) return;
            if (trails.SetWidth(-1) || target.Executor.SetFloat("trails.width", -1)) return;
            if (!trails.SetWidth(0.2f) || trails.GetWidth() != 0.2f) return;
            if (!trails.SetColor(Color.red) || trails.GetColor() != Color.red) return;
            if (!target.Executor.Initialize(4)) return;
            target.Executor.SetVector("lifetime.range", new Vector4(0.01f, 0.01f, 0, 0));
            target.Executor.Step(0, 0, 1, 17);
            trailOldPoint = target.Executor.GetTrailPoint(0);
            target.Executor.Step(0.011f, 0, 0, 0);
            target.Executor.Step(0.005f, 0, 1, 17);
            resultTexture = target.Executor.TrailMetadata;
            passed = trailOldPoint != null && resultTexture != null;
        }

        public void VerifyTrailsOff()
        {
            passed = false;
            if (!target.Modules.Trails.SetEnabled(false)) return;
            target.Executor.Step(0.001f, 0, 0, 0);
            passed = true;
        }

        public void VerifyTrailsResume()
        {
            passed = false;
            if (!target.Modules.Trails.SetEnabled(true)) return;
            target.Executor.Step(0, 0, 0, 0);
            trailOldPoint = target.Executor.GetTrailPoint(1);
            resultTexture = target.Executor.TrailMetadata;
            passed = trailOldPoint != null && resultTexture != null;
        }

        public void VerifyTrailsDuration()
        {
            passed = false;
            var trails = target.Modules.Trails;
            if (trails.SetDuration(0) || trails.SetDuration(float.PositiveInfinity)) return;
            if (target.Executor.SetFloat("trails.duration", 0) || target.Executor.SetFloat("trails.duration", float.Epsilon)) return;
            if (!trails.SetDuration(1.25f) || trails.GetDuration() != 1.25f) return;
            target.Executor.Step(0, 0, 0, 0);
            trailOldPoint = target.Executor.GetTrailPoint(1);
            resultTexture = target.Executor.TrailMetadata;
            passed = target.Executor.GetTrailPoint(7) != null && target.Executor.GetTrailPoint(8) == null;
        }

        public void VerifyBlink()
        {
            passed = false;
            target.Pause();
            var blink = target.Modules.Blink;
            if (blink == null || blink != target.Modules.Blink || empty.Modules.Blink != null) return;
            if (!target.SetRenderIntensity(3) || target.GetRenderIntensity() != 3) return;
            if (target.SetRenderIntensity(float.NaN) || target.SetRenderIntensity(-1)) return;
            if (!blink.SetFrequency(4) || blink.GetFrequency() != 4) return;
            if (!blink.SetDuty(0.25f) || blink.GetDuty() != 0.25f) return;
            if (!blink.SetMinimum(0.1f) || blink.GetMinimum() != 0.1f) return;
            if (!blink.SetSmoothness(0.5f) || blink.GetSmoothness() != 0.5f) return;
            if (!blink.SetRandomPhase(1) || blink.GetRandomPhase() != 1) return;
            if (!blink.SetTimeMode(FluxParticleBlinkTimeMode.ParticleAge) || blink.GetTimeMode() != FluxParticleBlinkTimeMode.ParticleAge) return;
            if (!blink.SetTarget(FluxParticleBlinkTarget.Both) || blink.GetTarget() != FluxParticleBlinkTarget.Both) return;
            if (blink.SetDuty(2) || blink.SetFrequency(float.PositiveInfinity)) return;
            if (target.Executor.SetFloat("blink.duty", 2) || target.Executor.SetFloat("blink.frequency", -1)) return;
            if (blink.SetTimeMode((FluxParticleBlinkTimeMode)99) || target.Executor.SetFloat("blink.time-mode", 99)) return;
            if (blink.SetTarget((FluxParticleBlinkTarget)(-1)) || target.Executor.SetFloat("blink.target", 0.5f)) return;
            if (blink.GetTarget() != FluxParticleBlinkTarget.Both || blink.GetDuty() != 0.25f) return;
            if (target.Executor.SetFloat("blink.enabled", 2)) return;
            if (!target.Executor.Initialize(4)) return;
            if (!blink.SetEnabled(false) || blink.GetEnabled()) return;
            if (!blink.SetEnabled(true) || !blink.GetEnabled()) return;
            target.Executor.Step(0, 0, 1, 1);
            target.Executor.Step(0.25f, 0, 0, 2);
            if (!target.SetRenderIntensity(2) || !blink.SetFrequency(1)) return;
            passed = true;
        }

        public void VerifyAliveCount()
        {
            target.Pause();
            var executor = target.Executor;
            aliveRequestAccepted = false;
            switch (aliveStage)
            {
                case 0:
                    if (!executor.Initialize(4)) return;
                    if (!executor.SetVector("lifetime.range", new Vector4(1, 1, 0, 0))) return;
                    executor.Step(0, 0, 3, 0);
                    aliveRequestAccepted = target.RequestAliveCount() && !target.RequestAliveCount();
                    break;
                case 1:
                    executor.Step(2, 0, 0, 0);
                    aliveRequestAccepted = target.RequestAliveCount();
                    break;
                case 2:
                    executor.Step(0, 0, 4, 0);
                    aliveRequestAccepted = target.RequestAliveCount();
                    executor.Clear();
                    break;
                case 3:
                    executor.Step(0, 0, 3, 0);
                    aliveRequestAccepted = target.RequestAliveCount();
                    executor.Release();
                    aliveRequestAccepted &= executor.Initialize(4);
                    break;
                case 4:
                    executor.Step(0, 0, 2, 0);
                    aliveRequestAccepted = target.RequestAliveCount();
                    break;
            }
        }

        public void ReadAliveCount()
        {
            aliveResult = target.AliveCount;
            alivePending = target.Executor.IsAliveCountPending;
        }

        bool VerifyStandardHandles()
        {
            var modules = target.Modules;
            if (modules.Drag == null || modules.Drag != modules.Drag || empty.Modules.Drag != null) return false;
            if (!modules.Drag.SetDrag(2f) || modules.Drag.GetDrag() != 2f) return false;
            if (!modules.Drag.SetEnabled(false) || modules.Drag.GetEnabled()) return false;
            if (modules.Force == null || modules.Force != modules.Force || empty.Modules.Force != null) return false;
            if (!modules.Force.SetForce(new Vector3(0, 0, 2)) || modules.Force.GetForce() != new Vector3(0, 0, 2)) return false;
            if (!modules.Force.SetEnabled(false) || modules.Force.GetEnabled()) return false;
            if (modules.Noise == null || modules.Noise != modules.Noise || empty.Modules.Noise != null) return false;
            if (!modules.Noise.SetStrength(2f) || modules.Noise.GetStrength() != 2f) return false;
            if (!modules.Noise.SetScale(2f) || modules.Noise.GetScale() != 2f) return false;
            if (!modules.Noise.SetSpeed(2f) || modules.Noise.GetSpeed() != 2f) return false;
            if (!modules.Noise.SetEnabled(false) || modules.Noise.GetEnabled()) return false;
            if (modules.Vortex == null || modules.Vortex != modules.Vortex || empty.Modules.Vortex != null) return false;
            if (!modules.Vortex.SetCenter(new Vector3(0, 0, 2)) || modules.Vortex.GetCenter() != new Vector3(0, 0, 2)) return false;
            if (!modules.Vortex.SetAxis(new Vector3(0, 0, 2)) || modules.Vortex.GetAxis() != new Vector3(0, 0, 2)) return false;
            if (!modules.Vortex.SetStrength(2f) || modules.Vortex.GetStrength() != 2f) return false;
            if (!modules.Vortex.SetEnabled(false) || modules.Vortex.GetEnabled()) return false;
            if (modules.LimitVelocity == null || modules.LimitVelocity != modules.LimitVelocity || empty.Modules.LimitVelocity != null) return false;
            if (!modules.LimitVelocity.SetMaxSpeed(2f) || modules.LimitVelocity.GetMaxSpeed() != 2f) return false;
            if (!modules.LimitVelocity.SetEnabled(false) || modules.LimitVelocity.GetEnabled()) return false;
            if (modules.VelocityOverLifetime == null || modules.VelocityOverLifetime != modules.VelocityOverLifetime || empty.Modules.VelocityOverLifetime != null) return false;
            if (!modules.VelocityOverLifetime.SetStart(new Vector3(0, 0, 2)) || modules.VelocityOverLifetime.GetStart() != new Vector3(0, 0, 2)) return false;
            if (!modules.VelocityOverLifetime.SetEnd(new Vector3(0, 0, 2)) || modules.VelocityOverLifetime.GetEnd() != new Vector3(0, 0, 2)) return false;
            if (!modules.VelocityOverLifetime.SetOrbital(new Vector3(0, 0, 2)) || modules.VelocityOverLifetime.GetOrbital() != new Vector3(0, 0, 2)) return false;
            if (!modules.VelocityOverLifetime.SetOffset(new Vector3(0, 0, 2)) || modules.VelocityOverLifetime.GetOffset() != new Vector3(0, 0, 2)) return false;
            if (!modules.VelocityOverLifetime.SetRadial(2f) || modules.VelocityOverLifetime.GetRadial() != 2f) return false;
            if (!modules.VelocityOverLifetime.SetEnabled(false) || modules.VelocityOverLifetime.GetEnabled()) return false;
            if (modules.ColorOverLifetime == null || modules.ColorOverLifetime != modules.ColorOverLifetime || empty.Modules.ColorOverLifetime != null) return false;
            if (!modules.ColorOverLifetime.SetEndColor(new Color(0.2f, 0.3f, 0.4f, 1)) || modules.ColorOverLifetime.GetEndColor() != new Color(0.2f, 0.3f, 0.4f, 1)) return false;
            if (!modules.ColorOverLifetime.HasEnabled()) return false;
            if (!modules.ColorOverLifetime.SetEnabled(false) || modules.ColorOverLifetime.GetEnabled()) return false;
            if (!modules.ColorOverLifetime.SetEnabled(true) || !modules.ColorOverLifetime.GetEnabled()) return false;
            if (modules.SizeOverLifetime == null || modules.SizeOverLifetime != modules.SizeOverLifetime || empty.Modules.SizeOverLifetime != null) return false;
            if (!modules.SizeOverLifetime.SetEndSize(2f) || modules.SizeOverLifetime.GetEndSize() != 2f) return false;
            if (modules.SizeOverLifetime.SetEndSize(-1) || target.Executor.SetFloat("size.end", -1)) return false;
            if (modules.SizeOverLifetime.SetEndSize(float.NaN) || target.Executor.SetFloat("size.end", float.PositiveInfinity)) return false;
            if (modules.SizeOverLifetime.GetEndSize() != 2f) return false;
            if (!target.Executor.SetFloat("size.end", 0) || modules.SizeOverLifetime.GetEndSize() != 0) return false;
            if (!modules.SizeOverLifetime.SetEndSize(2f)) return false;
            if (!modules.SizeOverLifetime.HasEnabled()) return false;
            if (!modules.SizeOverLifetime.SetEnabled(false) || modules.SizeOverLifetime.GetEnabled()) return false;
            if (!modules.SizeOverLifetime.SetEnabled(true) || !modules.SizeOverLifetime.GetEnabled()) return false;
            if (modules.RotationOverLifetime == null || modules.RotationOverLifetime != modules.RotationOverLifetime || empty.Modules.RotationOverLifetime != null) return false;
            if (!modules.RotationOverLifetime.SetEndRotation(new Vector3(0, 0, 2)) || modules.RotationOverLifetime.GetEndRotation() != new Vector3(0, 0, 2)) return false;
            if (!modules.RotationOverLifetime.HasEnabled()) return false;
            if (!modules.RotationOverLifetime.SetEnabled(false) || modules.RotationOverLifetime.GetEnabled()) return false;
            if (!modules.RotationOverLifetime.SetEnabled(true) || !modules.RotationOverLifetime.GetEnabled()) return false;
            if (modules.TextureSheetAnimation == null || modules.TextureSheetAnimation != modules.TextureSheetAnimation || empty.Modules.TextureSheetAnimation != null) return false;
            if (modules.TextureSheetAnimation.SetTiles(97, 172961) || target.Executor.SetVector("texture-sheet.tiles", new Vector4(97, 172961, 0, 0))) return false;
            if (modules.TextureSheetAnimation.SetTiles(16777217, 1)) return false;
            if (target.Executor.SetVector("texture-sheet.tiles", new Vector4(1.5f, 1, 0, 0))) return false;
            if (modules.Noise.SetScale(-1) || target.Executor.SetFloat("noise.scale", -1)) return false;
            if (modules.Vortex.SetStrength(-1) || target.Executor.SetFloat("vortex.strength", -1)) return false;
            if (modules.LimitVelocity.SetMaxSpeed(-1) || target.Executor.SetFloat("limit-velocity.max-speed", -1)) return false;
            if (!modules.TextureSheetAnimation.SetTiles(2, 3) || modules.TextureSheetAnimation.GetTiles() != new Vector2(2, 3)) return false;
            if (!modules.TextureSheetAnimation.SetCycles(2f) || modules.TextureSheetAnimation.GetCycles() != 2f) return false;
            if (!modules.TextureSheetAnimation.HasEnabled()) return false;
            if (!modules.TextureSheetAnimation.SetEnabled(false) || modules.TextureSheetAnimation.GetEnabled()) return false;
            if (!modules.TextureSheetAnimation.SetEnabled(true) || !modules.TextureSheetAnimation.GetEnabled()) return false;
            return true;
        }

        public void Verify()
        {
            passed = false;
            if (target.Main == null || target.Shape == null || target.Emission == null) return;
            if (target.Main != target.Main || target.Shape != target.Shape || target.Emission != target.Emission) return;
            if (!target.Main.SetStartLifetimeRange(new Vector2(4, 4)) || target.Main.GetStartLifetimeRange() != new Vector2(4, 4)) return;
            if (!target.Main.SetStartColor(Color.red) || target.Main.GetStartColor() != Color.red) return;
            if (!target.Main.SetStartSize(2) || target.Main.GetStartSize() != 2) return;
            if (!target.Main.SetStartSpeed(0) || target.Main.GetStartSpeed() != 0) return;
            if (target.Main.SetStartColorRange(Color.red, Color.blue) || target.Main.SetStartRotationRange(Vector3.zero, Vector3.zero)) return;
            if (target.Main.SetStartSpeedRange(Vector2.one) || target.Main.SetStartSizeRange(Vector2.one)) return;
            if (target.Shape.GetShapeType() != FluxParticleShapeType.Point || target.Shape.HasRadius()) return;
            if (!target.Shape.SetPosition(Vector3.zero) || target.Shape.SetRadius(2)) return;
            if (target.Shape.SetAngle(30) || target.Shape.SetSize(Vector3.one)) return;
            if (!target.Emission.SetRate(12) || target.Emission.GetRate() != 12 || target.Emission.SetRate(float.NaN)) return;
            if (!target.Emission.SetBursts(new[] { 0f }, new[] { 2 }) || target.Emission.GetBurstParticleCount(0) != 2) return;
            if (!target.Main.SetDuration(2) || target.Main.GetDuration() != 2) return;
            if (!target.Main.SetStartDelay(0.5f) || target.Main.GetStartDelay() != 0.5f) return;
            target.Main.SetLoop(false);
            if (target.Main.GetLoop()) return;
            if (!VerifyStandardHandles()) return;
            var gravity = target.Modules.Gravity;
            var lift = target.Modules.CustomLift();
            if (gravity == null || lift == null || empty.Modules.Gravity != null || empty.Modules.CustomLift() != null) return;
            if (gravity != target.Modules.Gravity || lift != target.Modules.CustomLift()) return;
            if (!gravity.SetGravity(new Vector3(0, -2, 0)) || !gravity.SetEnabled(true)) return;
            if (!lift.SetStrength(8) || !lift.SetDirection(Vector3.up) || !lift.SetEnabled(true)) return;
            if (gravity.GetGravity() != new Vector3(0, -2, 0) || lift.GetStrength() != 8 || lift.GetDirection() != Vector3.up) return;
            if (gravity.SetGravity(new Vector3(float.NaN, 0, 0)) || lift.SetStrength(float.PositiveInfinity)) return;
            if (target.Executor.SetVector("gravity", new Vector4(0, float.PositiveInfinity, 0, 0))) return;
            if (gravity.GetGravity() != new Vector3(0, -2, 0) || lift.GetStrength() != 8) return;
            if (!target.Executor.SetFloat("custom-lift.strength", 4) || lift.GetStrength() != 4) return;
            if (!lift.SetStrength(8)) return;
            if (!target.Executor.Initialize(4)) return;
            if (gravity.GetGravity() != new Vector3(0, -2, 0) || lift.GetStrength() != 8) return;
            target.Executor.SetFloat("spawn.speed", 0);
            target.Executor.Step(0, 0, 1, 1);
            target.Executor.Step(0.5f, 0, 0, 2);
            if (target.Main.SetStartSize(3) || !target.Main.SetStartSpeed(2)) return;
            if (target.Modules.TextureSheetAnimation.SetTiles(4, 4)) return;
            if (!target.Modules.TextureSheetAnimation.SetCycles(3)) return;
            resultTexture = target.Executor.GetCurrentPage(resultPage);
            passed = true;
        }
    }
}
