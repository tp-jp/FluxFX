using JetBrains.Annotations;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// コンパイル済みParticle Programの再生とEmissionを制御します。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleSystem : UdonSharpBehaviour
    {
        [SerializeField]
        [Min(1)]
        int particleCount = 64;

        [SerializeField]
        bool playOnAwake = true;

        [SerializeField]
        FluxParticleEmitter particleEmitter;

        [SerializeField]
        FluxParticleRenderer particleRenderer;

        [SerializeField]
        string compiledParameters;

        [SerializeField]
        [HideInInspector]
        FluxParticleExecutor particleExecutor;

        bool _programReady;
        bool _isPlaying;
        bool _isPaused;
        object _emission;

        /// <summary>
        /// GPU ProgramのExecutor
        /// </summary>
        [PublicAPI]
        public FluxParticleExecutor Executor => particleExecutor;

        /// <summary>
        /// Particle Slot数
        /// </summary>
        [PublicAPI]
        public int ParticleCount => particleCount;

        /// <summary>
        /// Emission RateのRuntime Parameter
        /// </summary>
        [PublicAPI]
        public FluxParticleEmissionParameters Emission => (FluxParticleEmissionParameters)_emission;

        /// <summary>
        /// 一時停止中かどうか
        /// </summary>
        [PublicAPI]
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Delay待機を含むEmissionの再生中かどうか
        /// </summary>
        [PublicAPI]
        public bool IsEmitting => _programReady && _isPlaying && !_isPaused && !particleEmitter.HasCompleted;

        /// <summary>
        /// Clear以降のシミュレーション経過秒数
        /// </summary>
        [PublicAPI]
        public float PlaybackTime => _programReady ? particleExecutor.PlaybackTime : 0;

        /// <summary>
        /// 一時停止を解除し、Particleの生成と更新を再開します。
        /// </summary>
        [PublicAPI]
        public void Play()
        {
            _isPlaying = true;
            _isPaused = false;
        }

        /// <summary>
        /// 新しいParticleの生成を停止します。既存Particleの更新は継続します。
        /// </summary>
        [PublicAPI]
        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
        }

        /// <summary>
        /// Emissionの再生位置を初期化し、生成を再開します。
        /// </summary>
        [PublicAPI]
        public void Restart()
        {
            particleEmitter.ResetPlayback();
            _isPlaying = true;
            _isPaused = false;
        }

        /// <summary>
        /// 全State Pageを初期化します。Emissionの再生位置は変更しません。
        /// </summary>
        [PublicAPI]
        public void Clear()
        {
            particleExecutor.Clear();
        }

        /// <summary>
        /// Particleの生成と更新を一時停止します。
        /// </summary>
        [PublicAPI]
        public void Pause()
        {
            _isPaused = true;
        }

        void Start()
        {
            if (!VRCJson.TryDeserializeFromJson(compiledParameters, out var compiled))
            {
                Logger.LogError("Particle Program playback parameters are invalid.", this);
                return;
            }

            var maximum = particleRenderer.MaximumParticleCount;
            if (particleCount < 1 || particleCount > maximum)
            {
                Logger.LogError($"Particle count must be in [1, {maximum}] for this render Mesh and numeric precision.", this);
                return;
            }
            InitializeEmitter(compiled.DataDictionary);
            _programReady = particleExecutor.Initialize(particleCount);
            if (!_programReady) return;

            particleRenderer.InitializeProgram(particleCount, particleExecutor.RenderMaterial);
            _isPlaying = playOnAwake;
        }

        void Update()
        {
            Simulate(Time.deltaTime);
        }

        void Simulate(float deltaTime)
        {
            if (!_programReady || _isPaused) return;
            particleEmitter.UpdateEmission(_isPlaying ? deltaTime : 0);
            particleExecutor.Step(deltaTime, particleEmitter.SpawnStart, particleEmitter.SpawnCount, particleEmitter.SpawnSeedStart);
        }

        void InitializeEmitter(DataDictionary parameters)
        {
            var playback = parameters["playback"].DataDictionary;
            var startDelay = (float)playback["startDelay"].Double;
            var duration = (float)playback["duration"].Double;
            var loop = playback["loop"].Boolean;

            var emission = parameters["emission"].DataDictionary;
            var emissionRate = (float)emission["rate"].Double;
            var bursts = emission["bursts"].DataList;
            var burstTimes = new float[bursts.Count];
            var burstCounts = new int[bursts.Count];

            for (var i = 0; i < bursts.Count; i++)
            {
                var burst = bursts[i].DataDictionary;
                burstTimes[i] = (float)burst["time"].Double;
                burstCounts[i] = (int)burst["count"].Double;
            }

            _emission = FluxParticleEmissionParameters.New(emissionRate);
            particleEmitter.Initialize(Emission, burstTimes, burstCounts, particleCount, startDelay, duration, loop);
        }
    }
}
