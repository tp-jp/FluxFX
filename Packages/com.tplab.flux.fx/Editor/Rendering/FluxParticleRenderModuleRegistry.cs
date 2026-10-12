using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts.Modules;
using UnityEditor;

namespace TpLab.Flux.FX.Editor.Rendering
{
    /// <summary>
    /// Rendering Module型とDefinitionの対応を管理します。
    /// GPU Simulation用Registryとは独立しており、描画処理にHLSL EntryPointを要求しません。
    /// </summary>
    public sealed class FluxParticleRenderModuleRegistry
    {
        readonly Dictionary<Type, IFluxParticleRenderModuleDefinition> _definitions = new Dictionary<Type, IFluxParticleRenderModuleDefinition>();
        readonly HashSet<string> _moduleIds = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 登録済みRendering Definitionの数です。
        /// </summary>
        public int Count => _definitions.Count;

        /// <summary>
        /// 利用可能なRendering Definitionを検出して登録します。
        /// 同一Module型またはModuleIdの重複を許可しません。
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Definitionが不正な場合、または登録が重複した場合に発生します。
        /// </exception>
        public FluxParticleRenderModuleRegistry()
        {
            var types = TypeCache.GetTypesDerivedFrom<IFluxParticleRenderModuleDefinition>();

            foreach (var type in types)
            {
                if (type.IsAbstract || type.ContainsGenericParameters) continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException($"Render Module Definition requires a public parameterless constructor: {type.FullName}");
                }

                var definition = (IFluxParticleRenderModuleDefinition)Activator.CreateInstance(type);

                Register(definition);
            }
        }

        /// <summary>
        /// 指定したModule型に対応するRendering Definitionを検索します。
        /// </summary>
        /// <param name="moduleType">検索対象のModule型。</param>
        /// <param name="definition">一致するDefinition。存在しない場合はnull。</param>
        /// <returns>Definitionが登録されている場合はtrue。</returns>
        public bool TryGet(Type moduleType, out IFluxParticleRenderModuleDefinition definition)
        {
            return _definitions.TryGetValue(moduleType, out definition);
        }

        /// <summary>
        /// 指定したModule型に対応するRendering Definitionを取得します。
        /// </summary>
        /// <param name="moduleType">取得対象のModule型。</param>
        /// <returns>登録済みのRendering Definition。</returns>
        /// <exception cref="InvalidOperationException">
        /// Definitionが登録されていない場合に発生します。
        /// </exception>
        public IFluxParticleRenderModuleDefinition Get(Type moduleType)
        {
            if (TryGet(moduleType, out var definition)) return definition;

            throw new InvalidOperationException($"Render Module Definition is not registered: {moduleType?.FullName ?? "null"}");
        }

        /// <summary>
        /// 指定したModuleに対応するRendering Definitionを取得します。
        /// </summary>
        /// <param name="module">取得対象のModule。</param>
        /// <returns>登録済みのRendering Definition。</returns>
        public IFluxParticleRenderModuleDefinition Get(FluxParticleModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            return Get(module.GetType());
        }

        /// <summary>
        /// 登録済みのRendering DefinitionをModuleId順で取得します。
        /// </summary>
        /// <returns>ModuleIdの昇順に並んだDefinition一覧。</returns>
        public IReadOnlyList<IFluxParticleRenderModuleDefinition> GetAll()
        {
            var result = new List<IFluxParticleRenderModuleDefinition>(_definitions.Values);

            result.Sort((a, b) => string.CompareOrdinal(a.ModuleId, b.ModuleId));

            return result.AsReadOnly();
        }

        /// <summary>
        /// Definitionの型と識別子を検証し、Registryへ登録します。
        /// </summary>
        static void ValidateDefinition(IFluxParticleRenderModuleDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var moduleType = definition.ModuleType;

            if (moduleType == null ||
                moduleType.IsAbstract ||
                !typeof(FluxParticleModule).IsAssignableFrom(moduleType))
            {
                throw new InvalidOperationException($"Invalid Render Module type: {definition.GetType().FullName}");
            }

            if (string.IsNullOrWhiteSpace(definition.ModuleId))
            {
                throw new InvalidOperationException($"Render Module ID is empty: {moduleType.FullName}");
            }
        }

        /// <summary>
        /// Rendering Definitionを検証して登録します。
        /// </summary>
        void Register(IFluxParticleRenderModuleDefinition definition)
        {
            ValidateDefinition(definition);

            if (_definitions.ContainsKey(definition.ModuleType))
            {
                throw new InvalidOperationException($"Duplicate Render Module type: {definition.ModuleType.FullName}");
            }

            if (!_moduleIds.Add(definition.ModuleId))
            {
                throw new InvalidOperationException($"Duplicate Render Module ID: {definition.ModuleId}");
            }

            _definitions.Add(definition.ModuleType, definition);
        }
    }
}