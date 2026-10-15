using System;
using System.Collections.Generic;
using System.Linq;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Modules
{
    /// <summary>
    /// Editor内の標準・外部Module Definitionを発見し、対応するModule設定へ結び付けます。
    /// </summary>
    public sealed class FluxParticleModuleRegistry
    {
        readonly Dictionary<Type, IFluxParticleModuleDefinition> _definitions = new Dictionary<Type, IFluxParticleModuleDefinition>();
        readonly HashSet<string> _moduleIds = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Editorに読み込まれたModule Definitionを自動発見します。
        /// </summary>
        public FluxParticleModuleRegistry()
        {
            var types = TypeCache.GetTypesDerivedFrom<IFluxParticleModuleDefinition>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters)
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
            foreach (var type in types)
            {
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException($"Module Definition requires a public parameterless constructor: {type.FullName}");
                }

                Register((IFluxParticleModuleDefinition)Activator.CreateInstance(type));
            }
        }

        /// <summary>
        /// 指定したModule DefinitionでRegistryを初期化します。
        /// </summary>
        /// <param name="definitions">Module Definition一覧</param>
        public FluxParticleModuleRegistry(IEnumerable<IFluxParticleModuleDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            foreach (var definition in definitions)
            {
                Register(definition);
            }
        }

        /// <summary>
        /// 登録済みDefinitionをModule ID順で取得します。
        /// </summary>
        /// <returns>Definition一覧の読み取り専用スナップショット</returns>
        public IReadOnlyList<IFluxParticleModuleDefinition> GetAll()
        {
            return Array.AsReadOnly(_definitions.Values.OrderBy(definition => definition.ModuleId, StringComparer.Ordinal).ToArray());
        }

        /// <summary>
        /// Module型からDefinitionを解決します。
        /// </summary>
        public bool TryGet(Type moduleType, out IFluxParticleModuleDefinition definition)
        {
            if (moduleType == null)
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(moduleType, out definition);
        }

        /// <summary>
        /// 登録済みModuleからContributionを収集します。未登録・重複したModule設定は拒否します。
        /// 無効なModuleも登録の存在は検証し、Definitionの実行のみ省略します。
        /// </summary>
        /// <param name="modules">AuthoringのModule設定一覧</param>
        /// <param name="context">Coreと共有する論理Program Context</param>
        public void Contribute(IEnumerable<FluxParticleModule> modules, FluxParticleProgramContribution context)
        {
            if (modules == null)
            {
                throw new ArgumentNullException(nameof(modules));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var seen = new HashSet<Type>();
            foreach (var module in modules)
            {
                if (module == null)
                {
                    throw new InvalidOperationException("Missing Particle Module definition.");
                }

                var type = module.GetType();
                if (!seen.Add(type))
                {
                    throw new InvalidOperationException($"Duplicate Particle Module: {type.FullName}");
                }

                if (!TryGet(type, out var definition))
                {
                    throw new InvalidOperationException($"Unregistered Particle Module: {type.FullName}");
                }

                if (!module.Enabled)
                {
                    continue;
                }

                definition.Contribute(module, context.ForModule(definition.ModuleId));
            }
        }

        /// <summary>
        /// Module型とIDが重複しないことを検証してDefinitionを登録します。
        /// </summary>
        public void Register(IFluxParticleModuleDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var type = definition.ModuleType;
            if (type == null || type.IsAbstract || !typeof(FluxParticleModule).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Invalid Module type: {type?.FullName ?? "null"}");
            }

            if (string.IsNullOrWhiteSpace(definition.ModuleId))
            {
                throw new InvalidOperationException($"Module ID is required: {type.FullName}");
            }

            if (_definitions.ContainsKey(type))
            {
                throw new InvalidOperationException($"Duplicate Module type: {type.FullName}");
            }

            if (!_moduleIds.Add(definition.ModuleId))
            {
                throw new InvalidOperationException($"Duplicate Module ID: {definition.ModuleId}");
            }

            _definitions.Add(type, definition);
        }
    }
}