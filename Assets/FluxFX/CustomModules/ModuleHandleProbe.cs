using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
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
            if (modules.ColorOverLifetime.HasEnabled() || modules.ColorOverLifetime.SetEnabled(false)) return false;
            if (modules.SizeOverLifetime == null || modules.SizeOverLifetime != modules.SizeOverLifetime || empty.Modules.SizeOverLifetime != null) return false;
            if (!modules.SizeOverLifetime.SetEndSize(2f) || modules.SizeOverLifetime.GetEndSize() != 2f) return false;
            if (modules.SizeOverLifetime.HasEnabled() || modules.SizeOverLifetime.SetEnabled(false)) return false;
            if (modules.RotationOverLifetime == null || modules.RotationOverLifetime != modules.RotationOverLifetime || empty.Modules.RotationOverLifetime != null) return false;
            if (!modules.RotationOverLifetime.SetEndRotation(new Vector3(0, 0, 2)) || modules.RotationOverLifetime.GetEndRotation() != new Vector3(0, 0, 2)) return false;
            if (modules.RotationOverLifetime.HasEnabled() || modules.RotationOverLifetime.SetEnabled(false)) return false;
            if (modules.TextureSheetAnimation == null || modules.TextureSheetAnimation != modules.TextureSheetAnimation || empty.Modules.TextureSheetAnimation != null) return false;
            if (!modules.TextureSheetAnimation.SetTiles(2, 3) || modules.TextureSheetAnimation.GetTiles() != new Vector2(2, 3)) return false;
            if (!modules.TextureSheetAnimation.SetCycles(2f) || modules.TextureSheetAnimation.GetCycles() != 2f) return false;
            if (modules.TextureSheetAnimation.HasEnabled() || modules.TextureSheetAnimation.SetEnabled(false)) return false;
            return true;
        }

        public void Verify()
        {
            passed = false;
            if (!VerifyStandardHandles()) return;
            var gravity = target.Modules.Gravity;
            var lift = target.Modules.CustomLift();
            if (gravity == null || lift == null || empty.Modules.Gravity != null || empty.Modules.CustomLift() != null) return;
            if (gravity != target.Modules.Gravity || lift != target.Modules.CustomLift()) return;
            if (!gravity.SetGravity(new Vector3(0, -2, 0)) || !gravity.SetEnabled(true)) return;
            if (!lift.SetStrength(8) || !lift.SetDirection(Vector3.up) || !lift.SetEnabled(true)) return;
            if (gravity.GetGravity() != new Vector3(0, -2, 0) || lift.GetStrength() != 8 || lift.GetDirection() != Vector3.up) return;
            if (gravity.SetGravity(new Vector3(float.NaN, 0, 0)) || lift.SetStrength(float.PositiveInfinity)) return;
            if (gravity.GetGravity() != new Vector3(0, -2, 0) || lift.GetStrength() != 8) return;
            if (!target.Executor.SetFloat("custom-lift.strength", 4) || lift.GetStrength() != 4) return;
            if (!lift.SetStrength(8)) return;
            if (!target.Executor.Initialize(4)) return;
            if (gravity.GetGravity() != new Vector3(0, -2, 0) || lift.GetStrength() != 8) return;
            target.Executor.SetFloat("spawn.speed", 0);
            target.Executor.Step(0, 0, 1, 1);
            target.Executor.Step(0.5f, 0, 0, 2);
            if (target.Modules.TextureSheetAnimation.SetTiles(4, 4)) return;
            if (!target.Modules.TextureSheetAnimation.SetCycles(3)) return;
            resultTexture = target.Executor.GetCurrentPage(resultPage);
            passed = true;
        }
    }
}
