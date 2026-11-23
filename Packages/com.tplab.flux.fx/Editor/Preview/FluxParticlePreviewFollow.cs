using System;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Udon;
using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.FX.Editor.Preview
{
    /// <summary>
    /// 実行時設定とは独立したPreviewの追従先
    /// </summary>
    public enum FluxParticlePreviewFollowMode
    {
        Placement,
        SceneCamera,
        SpecifiedTransform
    }

    /// <summary>
    /// 保存されたTransformを書き換えずに、Runtimeと同じSystem追従の変換行列を求めます。
    /// </summary>
    public sealed class FluxParticlePreviewFollow
    {
        readonly FluxParticleAuthoring _authoring;
        Matrix4x4 _matrix;
        Quaternion _rotation;
        bool _ready;
        Vector3 _frozenPosition;
        Quaternion _frozenRotation;

        /// <summary>
        /// Preview専用の追従方式。既定は配置位置
        /// </summary>
        public FluxParticlePreviewFollowMode Mode { get; private set; }

        /// <summary>
        /// Preview専用の指定Transform。SceneやPrefabには保存しません。
        /// </summary>
        public Transform Target { get; private set; }

        /// <summary>
        /// 追従位置と向きを固定しているか。シミュレーションの停止とは独立
        /// </summary>
        public bool IsFrozen { get; private set; }

        /// <summary>
        /// 現在の追従先を取得できるか
        /// </summary>
        public bool HasTarget => Mode == FluxParticlePreviewFollowMode.Placement || ResolveTarget() != null;

        /// <summary>
        /// Previewの追従先を切り替え、位置の固定を解除します。Authoringには書き込みません。
        /// </summary>
        /// <param name="mode">Previewの追従方式</param>
        /// <param name="target">指定Transform方式の追従先</param>
        public void Configure(FluxParticlePreviewFollowMode mode, Transform target = null)
        {
            if (!Enum.IsDefined(typeof(FluxParticlePreviewFollowMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            if (Mode == mode && Target == target) return;
            Mode = mode;
            Target = target;
            IsFrozen = false;
            _ready = false;
            _matrix = _authoring.transform.localToWorldMatrix;
            _rotation = _authoring.transform.rotation;
        }

        /// <summary>
        /// 現在の追従位置と向きを固定、または解除します。粒子の再生は継続します。
        /// </summary>
        /// <param name="frozen">固定する場合はtrue</param>
        /// <returns>要求した固定状態へ変更できたか</returns>
        public bool SetFrozen(bool frozen)
        {
            if (IsFrozen == frozen) return true;
            if (frozen)
            {
                if (Mode == FluxParticlePreviewFollowMode.Placement || _authoring.SystemFollow.Target == FluxParticleFollowTarget.Off || !Refresh()) return false;
                _frozenPosition = _matrix.MultiplyPoint3x4(Vector3.zero);
                _frozenRotation = _rotation;
            }
            IsFrozen = frozen;
            return true;
        }

        internal Matrix4x4 Matrix
        {
            get
            {
                Refresh();
                return _matrix;
            }
        }

        internal FluxParticlePreviewFollow(FluxParticleAuthoring authoring)
        {
            _authoring = authoring;
            _matrix = authoring.transform.localToWorldMatrix;
            _rotation = authoring.transform.rotation;
        }

        internal bool Refresh()
        {
            var system = _authoring.transform;
            var follow = _authoring.SystemFollow;
            if (follow.Target == FluxParticleFollowTarget.Off || Mode == FluxParticlePreviewFollowMode.Placement)
            {
                _matrix = system.localToWorldMatrix;
                return true;
            }
            Vector3 position;
            if (IsFrozen)
            {
                position = _frozenPosition;
                _rotation = _frozenRotation;
            }
            else
            {
                var target = ResolveTarget();
                if (target == null) return _ready;
                position = FluxParticleFollow.Position(target.position, target.rotation, follow.PositionOffset, follow.OffsetSpace);
                _rotation = FluxParticleFollow.Rotation(target.rotation,
                    follow.Rotation == FluxParticleFollowRotation.None ? system.rotation : _rotation, follow.Rotation);
            }
            // 親の非一様スケールも、Runtimeでworld position/rotationだけを設定した場合と同じにします。
            var parent = system.parent;
            _matrix = parent == null ? Matrix4x4.TRS(position, _rotation, system.localScale) :
                parent.localToWorldMatrix * Matrix4x4.TRS(parent.InverseTransformPoint(position),
                    Quaternion.Inverse(parent.rotation) * _rotation, system.localScale);
            _ready = true;
            return true;
        }

        Transform ResolveTarget()
        {
            if (Mode == FluxParticlePreviewFollowMode.SpecifiedTransform) return Target;
            var view = SceneView.lastActiveSceneView;
            return view != null && view.camera != null ? view.camera.transform : null;
        }
    }
}
