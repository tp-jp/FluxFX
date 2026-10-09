using System;
using System.Collections.Generic;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public enum FluxParticleParameterType
    {
        Float,
        Vector
    }

    public sealed class FluxParticleParameterCollector
    {
        public sealed class Parameter
        {
            public string Name { get; }
            public FluxParticleParameterType Type { get; }
            public float FloatValue { get; }
            public Vector4 VectorValue { get; }

            public Parameter(string name, float value)
            {
                Name = name;
                Type = FluxParticleParameterType.Float;
                FloatValue = value;
            }

            public Parameter(string name, Vector4 value)
            {
                Name = name;
                Type = FluxParticleParameterType.Vector;
                VectorValue = value;
            }
        }

        readonly List<Parameter> _parameters = new List<Parameter>();
        readonly HashSet<string> _names = new HashSet<string>();

        public IReadOnlyList<Parameter> Parameters => _parameters;

        public void AddFloat(string name, float value)
        {
            Register(name);
            _parameters.Add(new Parameter(name, value));
        }

        public void AddVector(string name, Vector4 value)
        {
            Register(name);
            _parameters.Add(new Parameter(name, value));
        }

        public void Apply(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));

            foreach (var parameter in _parameters)
            {
                switch (parameter.Type)
                {
                    case FluxParticleParameterType.Float:
                        material.SetFloat(parameter.Name, parameter.FloatValue);
                        break;

                    case FluxParticleParameterType.Vector:
                        material.SetVector(parameter.Name, parameter.VectorValue);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported GPU parameter type: {parameter.Type}");
                }
            }
        }

        void Register(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("GPU parameter name cannot be empty.", nameof(name));
            }

            if (!_names.Add(name))
            {
                throw new InvalidOperationException($"Duplicate GPU parameter: {name}");
            }
        }
    }
}
