using JetBrains.Annotations;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;

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
        [HideInInspector]
        FluxParticleExecutor particleExecutor;

        [SerializeField]
        FluxParticleModules modules;

        /// <summary>
        /// 組み込み済みModuleへの操作入口
        /// </summary>
        public FluxParticleModules Modules => modules;

        bool _programReady;
        bool _isPlaying;
        bool _isPaused;
        object _emission;
        object _main;
        object _shape;

        [SerializeField]
        int[] mainParameterSlots;
        [SerializeField]
        int[] shapeParameterSlots;
        [SerializeField]
        FluxParticleSimulationSpace simulationSpace;
        [SerializeField]
        FluxParticleShapeType shapeType;
        [SerializeField]
        FluxParticleRenderMode renderMode;

        /// <summary>
        /// Emissionの管理先
        /// </summary>
        public FluxParticleEmitter Emitter => particleEmitter;
        public FluxParticleSimulationSpace SimulationSpace => simulationSpace;
        public FluxParticleShapeType ShapeType => shapeType;
        public FluxParticleRenderMode RenderMode => renderMode;

        /// <summary>
        /// Mainの操作ハンドル
        /// </summary>
        public FluxParticleMainHandle Main
        {
            get
            {
                if (particleExecutor == null || mainParameterSlots == null) return null;
                if (_main == null)
                {
                    _main = FluxParticleMainHandle.Create(this, mainParameterSlots);
                }
                return (FluxParticleMainHandle)_main;
            }
        }

        /// <summary>
        /// Shapeの操作ハンドル
        /// </summary>
        public FluxParticleShapeHandle Shape
        {
            get
            {
                if (particleExecutor == null || shapeParameterSlots == null) return null;
                if (_shape == null)
                {
                    _shape = FluxParticleShapeHandle.Create(this, shapeParameterSlots);
                }
                return (FluxParticleShapeHandle)_shape;
            }
        }

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
        public FluxParticleEmissionHandle Emission
        {
            get
            {
                if (particleEmitter == null) return null;
                if (_emission == null)
                {
                    _emission = FluxParticleEmissionHandle.Create(particleEmitter);
                }
                return (FluxParticleEmissionHandle)_emission;
            }
        }

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
            var maximum = particleRenderer.MaximumParticleCount;
            if (particleCount < 1 || particleCount > maximum)
            {
                Logger.LogError($"Particle count must be in [1, {maximum}] for this render Mesh and numeric precision.", this);
                return;
            }
            particleEmitter.Initialize(particleCount);
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
    }
}
