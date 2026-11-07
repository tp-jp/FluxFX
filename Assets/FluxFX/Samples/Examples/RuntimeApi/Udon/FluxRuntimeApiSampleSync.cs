using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TpLab.Flux.FX.Samples.RuntimeApi.Udon
{
    /// <summary>
    /// 操作権限を持つプレイヤーの設定と再生操作をManual同期します。
    /// 粒子の位置とシミュレーション経過時間は各クライアントで独立しています。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FluxRuntimeApiSampleSync : UdonSharpBehaviour
    {
        [SerializeField]
        FluxRuntimeApiSample sample;

        [SerializeField]
        CanvasGroup sharedControls;

        [SerializeField]
        Text ownerLabel;

        [UdonSynced]
        bool _initialized;

        [UdonSynced]
        float[] _values = new float[0];

        [UdonSynced]
        bool[] _modules = new bool[0];

        [UdonSynced]
        float[] _initialValues = new float[0];

        [UdonSynced]
        bool _loop;

        [UdonSynced]
        int _shape;

        [UdonSynced]
        int _playback;

        [UdonSynced]
        int _initialRevision;

        [UdonSynced]
        int _restartRevision;

        [UdonSynced]
        float _burstTime;

        [UdonSynced]
        int _burstCount;

        int _appliedInitial = -1;
        int _appliedRestart = -1;
        bool _pendingApply;
        bool _pendingSend;
        float _nextSend;
        float _nextStatus;

        /// <summary>
        /// 現在の共有状態を受信した後に操作権限を取得します。
        /// </summary>
        public void TakeControl()
        {
            if (!sample.IsReady || !_initialized || !Networking.IsNetworkSettled) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RefreshOwnership();
        }

        public void OnControlsChanged()
        {
            if (!CanEdit()) return;
            sample.OnControlsChanged();
            Capture(false, false);
        }

        public void OnShapeChanged()
        {
            if (!CanEdit()) return;
            sample.OnShapeChanged();
            _playback = 0;
            Capture(true, true);
        }

        public void ApplyInitialValues()
        {
            if (!CanEdit()) return;
            sample.ApplyInitialValues();
            _playback = 0;
            Capture(true, true);
        }

        public void TogglePlay()
        {
            if (!CanEdit()) return;
            _playback = _playback == 0 ? 1 : 0;
            sample.SetPlaybackState(_playback);
            Capture(false, false);
        }

        public void Stop()
        {
            if (!CanEdit()) return;
            _playback = 2;
            sample.Stop();
            Capture(false, false);
        }

        public void Restart()
        {
            if (!CanEdit()) return;
            _playback = 0;
            sample.Restart();
            Capture(false, true);
        }

        void Start()
        {
            sharedControls.interactable = false;
        }

        void Update()
        {
            if (!sample.IsReady) return;
            if (!_initialized && Networking.IsOwner(gameObject) && Networking.IsNetworkSettled)
            {
                _initialized = true;
                sample.SetPlaybackState(0);
                Capture(true, true);
            }
            if (_pendingApply && _initialized)
            {
                ApplyReceivedState();
            }
            if (_pendingSend && Networking.IsOwner(gameObject) && Time.time >= _nextSend)
            {
                _pendingSend = false;
                _nextSend = Time.time + 0.2f;
                RequestSerialization();
            }
            if (Time.time >= _nextStatus)
            {
                _nextStatus = Time.time + 0.5f;
                RefreshOwnership();
            }
        }

        public override void OnDeserialization()
        {
            _pendingApply = true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            RefreshOwnership();
            if (player.isLocal && _initialized)
            {
                _pendingApply = true;
                _pendingSend = true;
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                _pendingSend = true;
            }
        }

        bool CanEdit() => sample.IsReady && _initialized && !_pendingApply && Networking.IsOwner(gameObject);

        void Capture(bool initial, bool restart)
        {
            _values = sample.CaptureValues();
            _modules = sample.CaptureModules();
            _shape = sample.SelectedShape;
            _loop = sample.Loop;
            if (initial)
            {
                _initialValues = sample.CaptureValues();
                _initialRevision++;
            }
            if (restart)
            {
                _burstTime = _values[(int)SampleControl.BurstTime];
                _burstCount = (int)_values[(int)SampleControl.BurstCount];
                _restartRevision++;
            }
            _appliedInitial = _initialRevision;
            _appliedRestart = _restartRevision;
            _pendingSend = true;
        }

        void ApplyReceivedState()
        {
            sample.ApplySharedState(_values, _modules, _loop, _shape, _initialValues,
                _appliedInitial != _initialRevision, _appliedRestart != _restartRevision, _playback, _burstTime, _burstCount);
            _appliedInitial = _initialRevision;
            _appliedRestart = _restartRevision;
            _pendingApply = false;
        }

        void RefreshOwnership()
        {
            sharedControls.interactable = CanEdit();
            var owner = Networking.GetOwner(gameObject);
            ownerLabel.text = !_initialized ? "Waiting for shared settings..." :
                "Control: " + (Utilities.IsValid(owner) ? owner.displayName : "--") +
                (Networking.IsOwner(gameObject) ? " (you)" : " / Take Control to edit");
        }
    }
}
