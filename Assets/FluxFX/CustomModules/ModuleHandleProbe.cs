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

        public void Verify()
        {
            passed = false;
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
            resultTexture = target.Executor.GetCurrentPage(resultPage);
            passed = true;
        }
    }
}
