using TpLab.Flux.FX.Udon;
using UnityEngine;
using VRC.Udon;

namespace TpLab.Flux.FX.Editor.Inspector
{
    /// <summary>
    /// 実行中のExecutorのUdon VMへLive操作を送ります。
    /// </summary>
    public static class FluxParticleLiveConnection
    {
        public static bool Request(UdonBehaviour executor, string id, int type, bool write,
            ref Vector4 value, out bool accepted)
        {
            accepted = false;
            if (executor == null || !executor.enabled || !executor.gameObject.activeInHierarchy) return false;
            executor.SetProgramVariable("_inspectorAvailable", false);
            executor.SetProgramVariable("_inspectorAccepted", false);
            executor.SetProgramVariable("_inspectorParameterId", id);
            executor.SetProgramVariable("_inspectorParameterType", type);
            executor.SetProgramVariable("_inspectorValue", value);
            executor.SetProgramVariable("_inspectorWrite", write);
            executor.SendCustomEvent(nameof(FluxParticleExecutor._ApplyInspectorRequest));
            if (!(executor.GetProgramVariable("_inspectorAvailable") is bool available) || !available) return false;
            accepted = executor.GetProgramVariable("_inspectorAccepted") is bool result && result;
            value = (Vector4)executor.GetProgramVariable("_inspectorValue");
            return true;
        }
    }
}
