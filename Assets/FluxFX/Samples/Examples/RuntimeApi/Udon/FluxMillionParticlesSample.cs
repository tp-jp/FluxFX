using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace TpLab.Flux.FX.Samples.RuntimeApi.Udon
{
    /// <summary>
    /// 1つのSystemで100万Particleを表示するサンプルを操作します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxMillionParticlesSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem system;

        [SerializeField]
        Slider rate;

        [SerializeField]
        Slider noise;

        [SerializeField]
        Slider intensity;

        [SerializeField]
        Text status;

        [SerializeField]
        Text rateLabel;

        [SerializeField]
        Text noiseLabel;

        [SerializeField]
        Text intensityLabel;

        [SerializeField]
        Text playLabel;

        bool _ready;
        float _nextStatus;

        /// <summary>
        /// Systemの初期化後、停止状態でUIを準備します。
        /// </summary>
        public void InitializeSample()
        {
            system.Pause();
            _ready = true;
            OnControlsChanged();
        }

        public void OnControlsChanged()
        {
            rateLabel.text = "Particles per second: " + rate.value.ToString("N0");
            noiseLabel.text = "Noise strength: " + noise.value.ToString("F2");
            intensityLabel.text = "Intensity: " + intensity.value.ToString("F2");
            if (!_ready) return;
            system.Emission.SetRate(rate.value);
            system.Modules.Noise.SetStrength(noise.value);
            system.SetRenderIntensity(intensity.value);
        }

        public void TogglePlay()
        {
            if (!_ready) return;
            if (system.IsPaused || !system.IsEmitting)
            {
                system.Play();
            }
            else
            {
                system.Pause();
            }
        }

        public void Stop()
        {
            if (!_ready) return;
            system.Stop();
        }

        public void Restart()
        {
            if (!_ready) return;
            system.Clear();
            system.Restart();
        }

        void Start()
        {
            SendCustomEventDelayedFrames(nameof(InitializeSample), 2);
        }

        void Update()
        {
            if (!_ready || Time.time < _nextStatus) return;
            _nextStatus = Time.time + 0.5f;
            system.RequestAliveCount();
            var alive = system.AliveCount;
            status.text = "Alive " + (alive < 0 ? "--" : alive.ToString("N0")) +
                " / " + system.ParticleCount.ToString("N0") + " slots\n" +
                "Playback " + system.PlaybackTime.ToString("F1") + " s";
            playLabel.text = system.IsPaused || !system.IsEmitting ? "Play" : "Pause";
        }
    }
}
