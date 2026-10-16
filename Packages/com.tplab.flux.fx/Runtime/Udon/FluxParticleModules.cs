using UnityEngine;
using UdonSharp;
using VRC.SDK3.Data;
using TpLab.Flux.FX.Udon.Modules;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// 組み込み済みModuleの操作ハンドルをSystemごとに保持します。
    /// </summary>
    /// <remarks>
    /// 標準ModuleはSystem.Modulesの各プロパティ、カスタムModuleはサンプルのCustomLift()のような拡張メソッドから取得します。
    /// 未組み込みのModuleはnullです。操作前にnullを確認してください。OFFで組み込まれたModuleは取得できます。
    /// 同じSystem・Bindingではハンドルを再利用します。RestartやClearで取得し直す必要はありません。
    /// ビルドによる構成変更、Systemの置き換え、Playの再開始後は取得し直してください。
    /// </remarks>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleModules : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleExecutor executor;
        DataDictionary handles;

        /// <summary>
        /// Parameterの管理先
        /// </summary>
        public FluxParticleExecutor Executor => executor;

        /// <summary>
        /// Gravityの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleGravityHandle Gravity => FluxParticleGravityHandle.Get(this);

        /// <summary>
        /// Dragの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleDragHandle Drag => FluxParticleDragHandle.Get(this);

        /// <summary>
        /// Forceの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleForceHandle Force => FluxParticleForceHandle.Get(this);

        /// <summary>
        /// Noiseの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleNoiseHandle Noise => FluxParticleNoiseHandle.Get(this);

        /// <summary>
        /// Vortexの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleVortexHandle Vortex => FluxParticleVortexHandle.Get(this);

        /// <summary>
        /// LimitVelocityの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleLimitVelocityHandle LimitVelocity => FluxParticleLimitVelocityHandle.Get(this);

        /// <summary>
        /// VelocityOverLifetimeの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleVelocityOverLifetimeHandle VelocityOverLifetime => FluxParticleVelocityOverLifetimeHandle.Get(this);

        /// <summary>
        /// ColorOverLifetimeの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleColorOverLifetimeHandle ColorOverLifetime => FluxParticleColorOverLifetimeHandle.Get(this);

        /// <summary>
        /// SizeOverLifetimeの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleSizeOverLifetimeHandle SizeOverLifetime => FluxParticleSizeOverLifetimeHandle.Get(this);

        /// <summary>
        /// RotationOverLifetimeの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleRotationOverLifetimeHandle RotationOverLifetime => FluxParticleRotationOverLifetimeHandle.Get(this);

        /// <summary>
        /// TextureSheetAnimationの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleTextureSheetAnimationHandle TextureSheetAnimation => FluxParticleTextureSheetAnimationHandle.Get(this);

        /// <summary>
        /// Blinkの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleBlinkHandle Blink => FluxParticleBlinkHandle.Get(this);

        /// <summary>
        /// Trailsの操作ハンドル。未組み込みの場合はnull
        /// </summary>
        public FluxParticleTrailsHandle Trails => FluxParticleTrailsHandle.Get(this);

        [SerializeField]
        string[] bindingIds;
        [SerializeField]
        int[] bindingOffsets;
        [SerializeField]
        int[] bindingSlots;
        [SerializeField]
        int[] enabledSlots;

        /// <summary>
        /// Moduleのビルド済みBindingを取得します。
        /// </summary>
        /// <returns>Executor参照と解決済みスロット。未対応または未組み込みの場合はnull</returns>
        internal DataList GetBinding(string moduleId)
        {
            if (bindingIds == null) return null;
            for (var i = 0; i < bindingIds.Length; i++)
            {
                if (bindingIds[i] != moduleId) continue;
                var binding = new DataList();
                binding.Add(new DataToken(executor));
                binding.Add(enabledSlots[i]);
                for (var slot = bindingOffsets[i]; slot < bindingOffsets[i + 1]; slot++)
                {
                    binding.Add(bindingSlots[slot]);
                }
                return binding;
            }
            return null;
        }

        /// <summary>
        /// キャッシュ済みのModuleハンドルを取得します。
        /// </summary>
        internal DataList GetCachedHandle(string moduleId)
        {
            if (handles != null && handles.TryGetValue(moduleId, out var token)) return token.DataList;
            return null;
        }

        /// <summary>
        /// Moduleハンドルを初回取得時に登録します。
        /// </summary>
        internal void CacheHandle(string moduleId, DataList handle)
        {
            if (handles == null)
            {
                handles = new DataDictionary();
            }
            handles[moduleId] = handle;
        }
    }
}
