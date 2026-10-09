using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Editor.GpuModules;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Inspector
{
    public partial class FluxParticleSystemEditor
    {
        List<ModuleDefinition> GetCustomVelocityModuleDefinitions()
        {
            var result = new List<ModuleDefinition>();
            var registry = new FluxParticleGpuModuleRegistry();

            foreach (var definition in registry.GetAll())
            {
                var type = definition.ModuleType;

                if (IsStandardModule(type)) continue;
                if (type.IsAbstract || type.ContainsGenericParameters) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                var instance = (FluxParticleModule)Activator.CreateInstance(type);
                if (instance.Stage != FluxParticleExecutionStage.VelocityUpdate) continue;

                var label = UnityEditor.ObjectNames.NicifyVariableName(type.Name);
                var expandedKey = SessionStatePrefix + "CustomModule." + type.FullName;

                result.Add(new ModuleDefinition(type, label, expandedKey));
            }

            return result;
        }

        void DrawCustomVelocityModules()
        {
            foreach (var definition in GetCustomVelocityModuleDefinitions())
            {
                var index = FindModuleIndex(definition.Type);
                if (index < 0) continue;

                var module = _modules.GetArrayElementAtIndex(index);
                var expanded = SessionState.GetBool(definition.ExpandedKey, false);

                DrawModule(
                    definition.Label,
                    module,
                    expanded,
                    definition.ExpandedKey,
                    () => ShowRemoveModuleMenu(definition.Type));
            }
        }

        static bool IsStandardModule(Type type)
        {
            foreach (var definition in VelocityModuleDefinitions)
            {
                if (definition.Type == type) return true;
            }

            foreach (var definition in RendererModuleDefinitions)
            {
                if (definition.Type == type) return true;
            }

            return false;
        }
    }
}
