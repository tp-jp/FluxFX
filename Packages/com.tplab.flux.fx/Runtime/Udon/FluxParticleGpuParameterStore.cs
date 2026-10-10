using JetBrains.Annotations;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon
{
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleGpuParameterStore : UdonSharpBehaviour
    {
        const string ValueKey = "value";
        const string TypeKey = "type";
        const string DirtyKey = "dirty";

        const int FloatType = 0;
        const int VectorType = 1;

        DataDictionary _parameters;
        FluxKernel _kernel;
        int _dirtyCount;

        [PublicAPI]
        public int Count => _parameters == null ? 0 : _parameters.Count;

        [PublicAPI]
        public int PendingCount => _dirtyCount;

        [PublicAPI]
        public void Initialize(FluxKernel kernel)
        {
            if (kernel == null)
            {
                Logger.LogError("GPU Parameter Store requires a FluxKernel.");
                return;
            }

            _kernel = kernel;
            _parameters = new DataDictionary();
            _dirtyCount = 0;
        }

        [PublicAPI]
        public bool RegisterFloat(string name, float value)
        {
            if (!CanRegister(name)) return false;

            var parameter = CreateParameter(new DataToken(value), FloatType);
            _parameters.Add(name, parameter);
            _dirtyCount++;

            return true;
        }

        [PublicAPI]
        public bool RegisterVector(string name, Vector4 value)
        {
            if (!CanRegister(name)) return false;

            var parameter = CreateParameter(new DataToken(value), VectorType);
            _parameters.Add(name, new DataToken(parameter));
            _dirtyCount++;

            return true;
        }

        [PublicAPI]
        public bool SetFloat(string name, float value)
        {
            if (!TryGetParameter(name, FloatType, out var parameter)) return false;

            var previous = (float)parameter[ValueKey].Double;
            if (previous == value) return true;

            parameter.SetValue(ValueKey, value);
            MarkDirty(parameter);

            return true;
        }

        [PublicAPI]
        public bool SetVector(string name, Vector4 value)
        {
            if (!TryGetParameter(name, VectorType, out var parameter)) return false;

            var previous = (Vector4)parameter[ValueKey].Reference;
            if (previous == value) return true;

            parameter.SetValue(ValueKey, new DataToken(value));
            MarkDirty(parameter);

            return true;
        }

        [PublicAPI]
        public float GetFloat(string name)
        {
            if (!TryGetParameter(name, FloatType, out var parameter)) return 0.0f;

            return (float)parameter[ValueKey].Double;
        }

        [PublicAPI]
        public Vector4 GetVector(string name)
        {
            if (!TryGetParameter(name, VectorType, out var parameter)) return Vector4.zero;

            return (Vector4)parameter[ValueKey].Reference;
        }

        [PublicAPI]
        public int Flush()
        {
            if (_parameters == null || _dirtyCount == 0) return 0;

            var keys = _parameters.GetKeys();
            var appliedCount = 0;

            for (var i = 0; i < keys.Count; i++)
            {
                var name = keys[i].String;
                var parameter = _parameters[name].DataDictionary;

                if (!parameter[DirtyKey].Boolean) continue;

                var type = parameter[TypeKey].Int;
                var value = parameter[ValueKey];

                switch (type)
                {
                    case FloatType:
                        _kernel.SetFloat(name, (float)value.Double);
                        break;

                    case VectorType:
                        _kernel.SetVector(name, (Vector4)value.Reference);
                        break;

                    default:
                        Logger.LogError($"Unsupported GPU parameter type: {type}");
                        continue;
                }

                parameter[DirtyKey] = false;
                _dirtyCount--;
                appliedCount++;
            }

            return appliedCount;
        }

        DataDictionary CreateParameter(DataToken value, int type)
        {
            var parameter = new DataDictionary();

            parameter[ValueKey] = value;
            parameter[TypeKey] = type;
            parameter[DirtyKey] = true;

            return parameter;
        }

        bool CanRegister(string name)
        {
            if (_parameters == null)
            {
                Logger.LogError("GPU Parameter Store is not initialized.");
                return false;
            }

            if (string.IsNullOrEmpty(name))
            {
                Logger.LogError("GPU parameter name is empty.");
                return false;
            }

            if (_parameters.ContainsKey(name))
            {
                Logger.LogError($"Duplicate GPU parameter: {name}");
                return false;
            }

            return true;
        }

        bool TryGetParameter(string name, int expectedType, out DataDictionary parameter)
        {
            parameter = null;

            if (_parameters == null || string.IsNullOrEmpty(name) || !_parameters.TryGetValue(name, out var token))
            {
                Logger.LogError($"GPU parameter is not registered: {name}");
                return false;
            }

            parameter = token.DataDictionary;

            if (parameter[TypeKey].Int == expectedType) return true;

            Logger.LogError($"GPU parameter type mismatch: {name}");
            parameter = null;

            return false;
        }

        void MarkDirty(DataDictionary parameter)
        {
            if (parameter[DirtyKey].Boolean) return;

            parameter.SetValue(DirtyKey, true);
            _dirtyCount++;
        }
    }
}