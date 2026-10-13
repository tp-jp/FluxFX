using System;
using System.Collections.Generic;
using TpLab.Flux.FX.Scripts;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 同一SceneFlow実行内でParticle SystemごとのBuild結果を共有するためのSessionです。
    /// </summary>
    public sealed class FluxFXCompilationSession
    {
        readonly Dictionary<FluxParticleAuthoring, FluxParticleBuildResult> _results = new Dictionary<FluxParticleAuthoring, FluxParticleBuildResult>();

        /// <summary>
        /// 登録済みのParticle System数を取得します。
        /// </summary>
        public int Count => _results.Count;

        /// <summary>
        /// 登録済みのParticle SystemのBuild結果を取得します。
        /// </summary>
        public IEnumerable<FluxParticleBuildResult> Results => _results.Values;

        /// <summary>
        /// Particle SystemのBuild結果を登録します。
        /// </summary>
        /// <param name="authoring">登録対象のParticle SystemのAuthoring。</param>
        /// <param name="result">登録するBuild結果。</param>
        /// <exception cref="ArgumentNullException">引数がnullの場合。</exception>
        /// <exception cref="InvalidOperationException">同じAuthoringが既に登録されている場合。</exception>
        public void Add(FluxParticleAuthoring authoring, FluxParticleBuildResult result)
        {
            if (authoring == null) throw new ArgumentNullException(nameof(authoring));
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (_results.ContainsKey(authoring))
            {
                throw new InvalidOperationException($"FluxFX compilation result is already registered: {authoring.name}");
            }

            _results.Add(authoring, result);
        }

        /// <summary>
        /// 指定したParticle SystemのBuild結果の取得を試みます。
        /// </summary>
        /// <param name="authoring">取得対象のParticle SystemのAuthoring。</param>
        /// <param name="result">取得したBuild結果。登録されていない場合はnull。</param>
        /// <returns>Build結果を取得できた場合はtrue、それ以外はfalse。</returns>
        /// <exception cref="ArgumentNullException">Authoringがnullの場合。</exception>
        public bool TryGet(FluxParticleAuthoring authoring, out FluxParticleBuildResult result)
        {
            if (authoring == null) throw new ArgumentNullException(nameof(authoring));

            return _results.TryGetValue(authoring, out result);
        }

        /// <summary>
        /// 指定したParticle SystemのBuild結果を取得します。
        /// </summary>
        /// <param name="authoring">取得対象のParticle SystemのAuthoring。</param>
        /// <returns>登録されているBuild結果。</returns>
        /// <exception cref="ArgumentNullException">Authoringがnullの場合。</exception>
        /// <exception cref="InvalidOperationException">Build結果が登録されていない場合。</exception>
        public FluxParticleBuildResult GetRequired(FluxParticleAuthoring authoring)
        {
            if (TryGet(authoring, out var result)) return result;

            throw new InvalidOperationException($"FluxFX compilation result is not registered: {authoring.name}");
        }
    }
}