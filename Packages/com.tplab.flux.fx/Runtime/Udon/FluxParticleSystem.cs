using JetBrains.Annotations;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// コンパイル済みParticle Programの再生とEmissionを制御します。
    /// GPU実行とState Page管理はFluxParticleExecutorへ委譲します。
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
        object _emission;

        /// <summary>
        /// GPU Programの実行と論理Parameterを管理するExecutorです。
        /// </summary>
        [PublicAPI]
        public FluxParticleExecutor Executor => particleExecutor;

        /// <summary>
        /// 使用するParticle Slotの数です。
        /// </summary>
        [PublicAPI]
        public int ParticleCount => particleCount;

        /// <summary>
        /// Emission RateをRuntimeから操作するためのParameterです。
        /// </summary>
        [PublicAPI]
        public FluxParticleEmissionParameters Emission => (FluxParticleEmissionParameters)_emission;

        /// <summary>
        /// Particleの生成を再開します。
        /// </summary>
        [PublicAPI]
        public void Play()
        {
            _isPlaying = true;
        }

        /// <summary>
        /// 新しいParticleの生成を停止します。既存Particleの更新は継続します。
        /// </summary>
        [PublicAPI]
        public void Stop()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// Emissionの再生位置を初期化し、生成を再開します。
        /// </summary>
        [PublicAPI]
        public void Restart()
        {
            particleEmitter.ResetPlayback();
            _isPlaying = true;
        }

        /// <summary>
        /// 全State Pageを初期化します。Emissionの再生位置は変更しません。
        /// </summary>
        [PublicAPI]
        public void Clear()
        {
            particleExecutor.Clear();
        }

        void Start()
        {
            if (!VRCJson.TryDeserializeFromJson(compiledParameters, out var compiled))
            {
                Logger.LogError("Particle Program playback parameters are invalid.", this);
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
            if (!_programReady) return;

            var deltaTime = Time.deltaTime;
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
