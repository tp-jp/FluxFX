using TpLab.Flux.FX.Udon.Parameters;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    /// <summary>
    /// コンパイル済みParticle ProgramをFluxKernelとFluxBufferで実行します。
    /// State LayoutはEditorが確定し、StepPreviousは全Passの終了まで保持します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleExecutor : UdonSharpBehaviour
    {
        [SerializeField]
        FluxKernel[] stepKernels;
        [SerializeField]
        FluxKernel[] clearKernels;
        [SerializeField]
        FluxBuffer[] pageBuffersA;
        [SerializeField]
        FluxBuffer[] pageBuffersB;
        [SerializeField]
        FluxBuffer spawnMaskBuffer;
        [SerializeField]
        FluxBuffer spawnKeyBuffer;
        [SerializeField]
        Material renderMaterial;
        [SerializeField]
        int[] outputPages;
        [SerializeField]
        bool[] spawnOnly;
        [SerializeField]
        int[] readOffsets;
        [SerializeField]
        int[] readPages;
        [SerializeField]
        bool[] readUpdated;
        [SerializeField]
        string[] readProperties;
        [SerializeField]
        string[] renderPageProperties;
        [SerializeField]
        string[] parameterIds;
        [SerializeField]
        string[] moduleIds;
        [SerializeField]
        string[] parameterProperties;
        [SerializeField]
        int[] parameterTypes;
        [SerializeField]
        bool[] staticParameters;
        [SerializeField]
        Vector4[] parameterValues;
        [SerializeField]
        int[] parameterConstraintFlags;
        [SerializeField]
        Vector4[] parameterMinimum;
        [SerializeField]
        Vector4[] parameterMaximum;
        [SerializeField]
        string deltaTimeProperty;
        [SerializeField]
        string simulationTimeProperty;
        [SerializeField]
        Transform emitterTransform;
        [SerializeField]
        string[] emitterRowProperties;
        [SerializeField]
        string spawnKeyProperty;

        [SerializeField]
        FluxKernel[] allocationKernels;
        [SerializeField]
        FluxBuffer[] allocationBuffers;
        [SerializeField]
        string[] allocationLookupProperties;
        [SerializeField]
        FluxReadback aliveCountReadback;

        [SerializeField]
        FluxKernel trailKernel;
        [SerializeField]
        int trailCapacity;
        [SerializeField]
        FluxBuffer[] trailBuffers;
        [SerializeField]
        Material[] trailMaterials;
        [SerializeField]
        MeshRenderer[] trailRenderers;
        [SerializeField]
        bool trailWorldSpace;
        float[] _trailState;
        bool _trailsEnabled;
        int _trailEnabledSlot;
        int _trailWidthSlot;
        int _trailColorSlot;
        int _trailDurationSlot;

        /// <summary>
        /// 軌跡の世代と生存状態を保持するTexture。未組み込みの場合はnull
        /// </summary>
        public RenderTexture TrailMetadata => trailKernel == null || _trailState == null ? null : trailBuffers[(int)_trailState[FluxParticleTrailHistory.MetadataIndex]].Texture;

        /// <summary>
        /// 最新の軌跡記録Textureを取得します。
        /// </summary>
        public RenderTexture GetTrailPoint(int age)
        {
            if (trailKernel == null || _trailState == null || age < 0 || age >= trailBuffers.Length - 2) return null;
            var index = ((int)_trailState[FluxParticleTrailHistory.HeadIndex] + trailBuffers.Length - 3 - age) % (trailBuffers.Length - 2);
            return trailBuffers[index].Texture;
        }

        FluxBuffer[] _current;
        FluxBuffer[] _next;
        bool[] _updated;
        int _count;
        bool _initialized;
        bool _started;
        float _simulationTime;
        const float AliveCountInterval = 0.2f;
        int _aliveCount = -1;
        bool _aliveCountPending;
        bool _discardAliveCount;
        float _nextAliveCountTime;

        /// <summary>
        /// 最後に取得した生存Particle数。未取得または取得失敗時は-1
        /// </summary>
        public int AliveCount => _aliveCount;

        /// <summary>
        /// 生存数の読み戻し処理中かどうか
        /// </summary>
        public bool IsAliveCountPending => _aliveCountPending;

        /// <summary>
        /// 現在のStateを集計し、生存数の非同期取得を要求します。要求間隔は0.2秒以上です。
        /// </summary>
        /// <returns>要求を開始した場合はtrue</returns>
        public bool RequestAliveCount()
        {
            if (!_initialized || aliveCountReadback == null || _aliveCountPending || aliveCountReadback.IsRequesting ||
                Time.realtimeSinceStartup < _nextAliveCountTime) return false;
            BindAvailability(0);
            FluxParticleSpawnAllocator.BuildCounts(allocationKernels, allocationBuffers);
            _aliveCountPending = true;
            _discardAliveCount = false;
            if (!aliveCountReadback.TryRequest(allocationBuffers[allocationBuffers.Length - 1], this))
            {
                _aliveCountPending = false;
                return false;
            }
            _nextAliveCountTime = Time.realtimeSinceStartup + AliveCountInterval;
            return true;
        }

        /// <summary>
        /// FluxReadbackの完了結果から生存数を更新します。
        /// </summary>
        public void _OnFluxReadbackComplete()
        {
            if (!_aliveCountPending) return;
            _aliveCountPending = false;
            if (!_initialized || _discardAliveCount) return;
            if (!aliveCountReadback.Succeeded || aliveCountReadback.Count != 1 || aliveCountReadback.Data == null ||
                aliveCountReadback.Data.Length != 1)
            {
                _aliveCount = -1;
                return;
            }
            var free = aliveCountReadback.Data[0].r;
            _aliveCount = float.IsNaN(free) || float.IsInfinity(free) ? -1
                : Mathf.Clamp(_count - Mathf.RoundToInt(free), 0, _count);
        }

        /// <summary>
        /// Particle System専用の描画Material
        /// </summary>
        public Material RenderMaterial => renderMaterial;

        /// <summary>
        /// Clear以降のシミュレーション経過秒数
        /// </summary>
        public float PlaybackTime => _simulationTime;

        /// <summary>
        /// FluxBufferを初期化し、確定済みManifestのGPU実行環境を構築します。
        /// </summary>
        /// <param name="count">有効Particle数</param>
        /// <returns>初期化に成功した場合はtrue</returns>
        public bool Initialize(int count)
        {
            if (_initialized) return true;
            if (count < 1 || count > FluxParticleCapacity.MaximumExactCount || !ValidateManifest())
            {
                Logger.LogError("Particle Executor Manifest is invalid.", this);
                return false;
            }

            _count = count;
            _current = new FluxBuffer[pageBuffersA.Length];
            _next = new FluxBuffer[pageBuffersB.Length];
            _updated = new bool[pageBuffersA.Length];

            for (var page = 0; page < _current.Length; page++)
            {
                _current[page] = pageBuffersA[page];
                _next[page] = pageBuffersB[page];
                if (!ConfigureBuffer(_current[page], count) || !ConfigureBuffer(_next[page], count))
                {
                    Release();
                    return false;
                }
            }
            if (!ConfigureBuffer(spawnMaskBuffer, count) || !ConfigureBuffer(spawnKeyBuffer, count))
            {
                Release();
                return false;
            }

            var texture = _current[0].Texture;
            var stateSize = new Vector4(texture.width, texture.height, count, 0);
            for (var page = 1; page < _current.Length; page++)
            {
                var current = _current[page].Texture;
                var next = _next[page].Texture;
                if (current.width != texture.width || current.height != texture.height ||
                    next.width != texture.width || next.height != texture.height)
                {
                    Logger.LogError("Particle State Page dimensions do not match.", this);
                    Release();
                    return false;
                }
            }
            if (spawnMaskBuffer.Texture.width != texture.width || spawnMaskBuffer.Texture.height != texture.height ||
                spawnKeyBuffer.Texture.width != texture.width || spawnKeyBuffer.Texture.height != texture.height)
            {
                Logger.LogError("Spawn Buffer dimensions do not match State Pages.", this);
                Release();
                return false;
            }

            renderMaterial.SetVector("_FluxStateSize", stateSize);
            if (trailKernel != null)
            {
                _trailState = new float[trailBuffers.Length - 2 + FluxParticleTrailHistory.SampleAgeOffset];
                _trailEnabledSlot = ResolveParameter("trails.enabled", 1);
                _trailWidthSlot = ResolveParameter("trails.width", 1);
                _trailColorSlot = ResolveParameter("trails.color", 4);
                _trailDurationSlot = ResolveParameter("trails.duration", 1);
                for (var i = 0; i < trailBuffers.Length; i++)
                {
                    if (!ConfigureBuffer(trailBuffers[i], Mathf.Min(count, trailCapacity)))
                    {
                        Release();
                        return false;
                    }
                }
                trailKernel.SetVector("_FluxStateSize", stateSize);
                for (var i = 0; i < allocationLookupProperties.Length; i++)
                {
                    var property = allocationLookupProperties[i];
                    trailKernel.SetTexture(property, renderMaterial.GetTexture(property));
                }
            }
            var allocationCount = count;
            for (var level = 0; level < allocationBuffers.Length; level++)
            {
                if (!ConfigureBuffer(allocationBuffers[level], allocationCount))
                {
                    Release();
                    return false;
                }
                allocationCount = (allocationCount + 3) / 4;
            }
            if (allocationBuffers[allocationBuffers.Length - 1].Count != 1)
            {
                Logger.LogError("Spawn allocation hierarchy is too shallow.", this);
                Release();
                return false;
            }
            FluxParticleSpawnAllocator.Configure(allocationKernels, allocationBuffers, spawnMaskBuffer, count);
            for (var i = 0; i < allocationLookupProperties.Length; i++)
            {
                var property = allocationLookupProperties[i];
                allocationKernels[0].SetTexture(property, renderMaterial.GetTexture(property));
            }
            _started = false;
            _initialized = true;

            for (var i = 0; i < parameterIds.Length; i++)
            {
                ApplyParameter(i, parameterValues[i]);
            }
            Clear();
            return true;
        }

        /// <summary>
        /// 全Pageの両Buffer、SpawnMask、Simulation Timeを初期状態に戻します。Static Parameterのロックは解除しません。
        /// </summary>
        public void Clear()
        {
            if (!_initialized) return;
            _simulationTime = 0;
            _aliveCount = 0;
            _discardAliveCount = true;
            _nextAliveCountTime = 0;

            for (var page = 0; page < _current.Length; page++)
            {
                clearKernels[page].Dispatch(_current[page]);
                clearKernels[page].Dispatch(_next[page]);
            }
            GenerateSpawnInputs(0, 0, 0, 0);
            if (trailKernel != null)
            {
                FluxParticleTrailHistory.Clear(trailKernel, trailBuffers, trailMaterials, _trailState);
                _trailsEnabled = GetParameterValue(_trailEnabledSlot).x > 0.5f;
                SetTrailRenderers(_trailsEnabled);
                FluxParticleTrailHistory.SetAppearance(trailMaterials, _trailsEnabled,
                    GetParameterValue(_trailWidthSlot).x, GetParameterValue(_trailColorSlot), GetParameterValue(_trailDurationSlot).x);
            }
            BindRender();
        }

        /// <summary>
        /// Spawn入力をGPUで生成し、空きSlotの初期化と生存Particleの更新を実行します。
        /// </summary>
        /// <param name="deltaTime">Simulationの経過時間</param>
        /// <param name="spawnStart">空きSlot順位の循環開始位置</param>
        /// <param name="spawnCount">生成要求数。空きSlotへ割り当て、空き数を超える要求は省略</param>
        /// <param name="seedStart">20bit SpawnKeyの先頭</param>
        public void Step(float deltaTime, int spawnStart, int spawnCount, int seedStart)
        {
            if (!_initialized) return;
            if (deltaTime < 0 || spawnStart < 0 || spawnStart >= _count || spawnCount < 0 || seedStart < 0 || seedStart >= 1048576) return;

            _started = true;
            GenerateSpawnInputs(spawnStart, spawnCount, seedStart, deltaTime);
            for (var page = 0; page < _updated.Length; page++)
            {
                _updated[page] = false;
            }

            for (var pass = 0; pass < stepKernels.Length; pass++)
            {
                if (spawnOnly[pass] && spawnCount == 0) continue;

                var kernel = stepKernels[pass];
                kernel.SetFloat(deltaTimeProperty, deltaTime);
                if (emitterRowProperties != null && emitterRowProperties.Length == 3)
                {
                    var matrix = emitterTransform.localToWorldMatrix;
                    for (var row = 0; row < 3; row++)
                    {
                        kernel.SetVector(emitterRowProperties[row], matrix.GetRow(row));
                    }
                }
                if (!string.IsNullOrEmpty(simulationTimeProperty))
                {
                    kernel.SetFloat(simulationTimeProperty, _simulationTime);
                }
                kernel.SetBuffer("_FluxSpawnMask", spawnMaskBuffer);
                kernel.SetBuffer(spawnKeyProperty, spawnKeyBuffer);

                for (var input = readOffsets[pass]; input < readOffsets[pass + 1]; input++)
                {
                    var page = readPages[input];
                    var buffer = readUpdated[input] && _updated[page] ? _next[page] : _current[page];
                    kernel.SetBuffer(readProperties[input], buffer);
                }

                var output = outputPages[pass];
                kernel.Dispatch(_next[output]);
                _updated[output] = true;
            }

            for (var page = 0; page < _current.Length; page++)
            {
                if (!_updated[page]) continue;
                var previous = _current[page];
                _current[page] = _next[page];
                _next[page] = previous;
            }
            _simulationTime += deltaTime;
            BindRender();
            if (_trailsEnabled)
            {
                var matrix = emitterTransform == null ? Matrix4x4.identity : emitterTransform.localToWorldMatrix;
                FluxParticleTrailHistory.Step(trailKernel, trailBuffers, trailMaterials, _trailState,
                    spawnMaskBuffer.Texture, _count, deltaTime, matrix, trailWorldSpace, GetParameterValue(_trailDurationSlot).x);
            }
        }

        /// <summary>
        /// 論理IDでFloat Parameterを変更します。
        /// </summary>
        /// <param name="id">Parameter ID</param>
        /// <param name="value">設定値</param>
        /// <returns>型とStatic制約を満たして設定できた場合はtrue</returns>
        public bool SetFloat(string id, float value)
        {
            var index = FindParameter(id);
            return SetParameterValue(index, 1, new Vector4(value, 0, 0, 0));
        }

        /// <summary>
        /// 論理IDでFloat2/3/4 Parameterを変更します。
        /// </summary>
        /// <param name="id">Parameter ID</param>
        /// <param name="value">設定するVector</param>
        /// <returns>型とStatic制約を満たして設定できた場合はtrue</returns>
        public bool SetVector(string id, Vector4 value)
        {
            var index = FindParameter(id);
            if (index < 0 || parameterTypes[index] < 2 || parameterTypes[index] > 4) return false;
            return SetParameterValue(index, parameterTypes[index], value);
        }

        /// <summary>
        /// ビルド済みProgramへのModuleの組み込み有無を返します。
        /// </summary>
        /// <param name="id">Module ID</param>
        /// <returns>組み込み済みの場合はtrue</returns>
        public bool HasModule(string id)
        {
            if (moduleIds == null) return false;
            for (var i = 0; i < moduleIds.Length; i++)
            {
                if (moduleIds[i] == id) return true;
            }
            return false;
        }

        /// <summary>
        /// IDと型に一致するParameterスロットを解決します。
        /// </summary>
        /// <param name="id">Parameter ID</param>
        /// <param name="type">Parameterの型</param>
        /// <returns>一致したスロット。見つからない場合は-1</returns>
        public int ResolveParameter(string id, int type)
        {
            var index = FindParameter(id);
            return index >= 0 && parameterTypes[index] == type ? index : -1;
        }

        /// <summary>
        /// Executorが保持する現在のParameter値を取得します。
        /// </summary>
        /// <param name="index">Parameterスロット</param>
        /// <returns>ParameterのVector4値。範囲外はVector4.zero</returns>
        public Vector4 GetParameterValue(int index)
        {
            if (parameterValues == null || index < 0 || index >= parameterValues.Length) return Vector4.zero;
            return parameterValues[index];
        }

        /// <summary>
        /// 解決済みスロットの型・Static制約・有限値を検証してParameterを変更します。
        /// </summary>
        /// <param name="index">Parameterスロット</param>
        /// <param name="type">Parameterの型</param>
        /// <param name="value">設定するVector4値</param>
        /// <returns>変更に成功した場合はtrue</returns>
        public bool SetParameterValue(int index, int type, Vector4 value)
        {
            if (!CanSetParameterValue(index, type, value)) return false;
            var previousValue = parameterValues[index];
            parameterValues[index] = value;
            if (_initialized)
            {
                ApplyParameter(index, value);
                if (trailKernel != null && (index == _trailEnabledSlot || index == _trailWidthSlot || index == _trailColorSlot || index == _trailDurationSlot))
                {
                    var enabled = GetParameterValue(_trailEnabledSlot).x > 0.5f;
                    if (enabled && (!_trailsEnabled || index == _trailDurationSlot && previousValue.x != value.x))
                    {
                        FluxParticleTrailHistory.Clear(trailKernel, trailBuffers, trailMaterials, _trailState);
                    }
                    if (enabled != _trailsEnabled)
                    {
                        _trailsEnabled = enabled;
                        SetTrailRenderers(enabled);
                    }
                    FluxParticleTrailHistory.SetAppearance(trailMaterials, enabled,
                        GetParameterValue(_trailWidthSlot).x, GetParameterValue(_trailColorSlot), GetParameterValue(_trailDurationSlot).x);
                }
            }
            return true;
        }

        /// <summary>
        /// Parameterを変更できるか、型・Static制約・有限値・契約の数値制約を検証します。
        /// </summary>
        /// <param name="index">Parameterスロット</param>
        /// <param name="type">Parameterの型</param>
        /// <param name="value">設定するVector4値</param>
        /// <returns>変更できる場合はtrue</returns>
        public bool CanSetParameterValue(int index, int type, Vector4 value)
        {
            if (parameterValues == null || index < 0 || index >= parameterValues.Length ||
                parameterTypes[index] != type || (_started && staticParameters[index])) return false;
            for (var component = 0; component < 4; component++)
            {
                if (float.IsNaN(value[component]) || float.IsInfinity(value[component])) return false;
            }
            if (parameterConstraintFlags == null || parameterMinimum == null || parameterMaximum == null ||
                index >= parameterConstraintFlags.Length || index >= parameterMinimum.Length || index >= parameterMaximum.Length) return false;
            var flags = parameterConstraintFlags[index];
            if (flags != 0)
            {
                for (var component = 0; component < type; component++)
                {
                    var number = value[component];
                    if ((flags & (int)FluxParticleNumericConstraintFlags.Minimum) != 0 &&
                        (number < parameterMinimum[index][component] ||
                         (flags & (int)FluxParticleNumericConstraintFlags.ExclusiveMinimum) != 0 && number == parameterMinimum[index][component])) return false;
                    if ((flags & (int)FluxParticleNumericConstraintFlags.Maximum) != 0 &&
                        (number > parameterMaximum[index][component] ||
                         (flags & (int)FluxParticleNumericConstraintFlags.ExclusiveMaximum) != 0 && number == parameterMaximum[index][component])) return false;
                    if ((flags & (int)FluxParticleNumericConstraintFlags.Integer) != 0 && Mathf.Floor(number) != number) return false;
                }
            }
            if (parameterIds[index] == "trails.duration" &&
                !FluxParticleTrailHistory.IsValidDuration(value.x, trailBuffers == null ? 2 : trailBuffers.Length - 2)) return false;
            return true;
        }

        /// <summary>
        /// 確定済みPageのRenderTextureを取得します。Stepをまたいで参照を保持しないでください。
        /// </summary>
        /// <param name="page">Compilerが確定したPage ID</param>
        /// <returns>該当PageのTexture。範囲外ならnull</returns>
        public RenderTexture GetCurrentPage(int page)
        {
            if (!_initialized || page < 0 || page >= _current.Length) return null;
            return _current[page].Texture;
        }

        /// <summary>
        /// Executorが保持するRuntime参照を解放します。
        /// 集計BufferのTextureを解放し、ビルド済みの構成は再初期化に備えて保持します。
        /// </summary>
        public void Release()
        {
            _aliveCount = -1;
            _discardAliveCount = true;
            _nextAliveCountTime = 0;
            if (allocationBuffers != null)
            {
                for (var level = 0; level < allocationBuffers.Length; level++)
                {
                    if (allocationBuffers[level] != null) allocationBuffers[level].Release();
                }
            }
            if (trailBuffers != null)
            {
                for (var i = 0; i < trailBuffers.Length; i++)
                {
                    if (trailBuffers[i] != null) trailBuffers[i].Release();
                }
                FluxParticleTrailHistory.SetAppearance(trailMaterials, false, 0, Vector4.zero, 0.5f);
            }
            SetTrailRenderers(false);
            _trailsEnabled = false;
            _trailState = null;
            _initialized = false;
            _current = null;
            _next = null;
            _updated = null;
        }

        void OnDestroy()
        {
            Release();
        }

        bool ConfigureBuffer(FluxBuffer buffer, int count)
        {
            if (buffer.Capacity < count)
            {
                Logger.LogError($"Particle Buffer capacity is too small: {buffer.Capacity} < {count}.", this);
                return false;
            }
            if (!buffer.Texture.IsCreated())
            {
                Logger.LogError("Particle Buffer RenderTexture allocation failed.", this);
                return false;
            }
            buffer.SetCount(count);
            return buffer.Count == count;
        }

        void GenerateSpawnInputs(int start, int count, int seed, float deltaTime)
        {
            var active = Mathf.Min(count, _count);
            var skipped = count - active;
            var first = (start + skipped % _count) % _count;
            var seedBase = (seed + skipped % 1048576) % 1048576;

            if (active > 0)
            {
                BindAvailability(deltaTime);
            }
            FluxParticleSpawnAllocator.Execute(allocationKernels, allocationBuffers, spawnMaskBuffer,
                spawnKeyBuffer, first, active, seedBase);
        }

        void BindAvailability(float deltaTime)
        {
            var availability = allocationKernels[0];
            availability.SetFloat(deltaTimeProperty, deltaTime);
            if (!string.IsNullOrEmpty(simulationTimeProperty))
            {
                availability.SetFloat(simulationTimeProperty, _simulationTime);
            }
            if (emitterRowProperties != null && emitterRowProperties.Length == 3)
            {
                var matrix = emitterTransform.localToWorldMatrix;
                for (var row = 0; row < 3; row++)
                {
                    availability.SetVector(emitterRowProperties[row], matrix.GetRow(row));
                }
            }
            for (var page = 0; page < _current.Length; page++)
            {
                availability.SetBuffer(renderPageProperties[page], _current[page]);
            }
        }

        int FindParameter(string id)
        {
            if (parameterIds == null) return -1;
            for (var i = 0; i < parameterIds.Length; i++)
            {
                if (parameterIds[i] == id) return i;
            }
            return -1;
        }

        void ApplyParameter(int index, Vector4 value)
        {
            var property = parameterProperties[index];
            for (var i = 0; i < stepKernels.Length; i++)
            {
                if (parameterTypes[index] == 1) stepKernels[i].SetFloat(property, value.x);
                else stepKernels[i].SetVector(property, value);
            }
            if (parameterTypes[index] == 1) allocationKernels[0].SetFloat(property, value.x);
            else allocationKernels[0].SetVector(property, value);
            if (trailKernel != null)
            {
                if (parameterTypes[index] == 1) trailKernel.SetFloat(property, value.x);
                else trailKernel.SetVector(property, value);
            }
            if (parameterTypes[index] == 1) renderMaterial.SetFloat(property, value.x);
            else renderMaterial.SetVector(property, value);
        }

        void SetTrailRenderers(bool enabled)
        {
            if (trailRenderers == null) return;
            for (var i = 0; i < trailRenderers.Length; i++)
            {
                if (trailRenderers[i] != null)
                {
                    trailRenderers[i].enabled = enabled;
                }
            }
        }

        void BindRender()
        {
            if (_trailsEnabled)
            {
                if (!string.IsNullOrEmpty(simulationTimeProperty)) trailKernel.SetFloat(simulationTimeProperty, _simulationTime);
            }
            if (!string.IsNullOrEmpty(simulationTimeProperty))
            {
                renderMaterial.SetFloat(simulationTimeProperty, _simulationTime);
            }
            for (var page = 0; page < _current.Length; page++)
            {
                renderMaterial.SetTexture(renderPageProperties[page], _current[page].Texture);
                if (_trailsEnabled)
                {
                    trailKernel.SetBuffer(renderPageProperties[page], _current[page]);
                }
            }
        }

        bool ValidateManifest()
        {
            if (trailKernel != null)
            {
                if (trailCapacity < 1 || trailCapacity > FluxParticleCapacity.MaximumExactCount ||
                    trailBuffers == null || trailBuffers.Length < 4 ||
                    trailMaterials == null || trailMaterials.Length != trailBuffers.Length - 3 ||
                    trailRenderers == null || trailRenderers.Length != trailMaterials.Length ||
                    ResolveParameter("trails.enabled", (int)FluxParticleValueType.Float) < 0 ||
                    ResolveParameter("trails.width", (int)FluxParticleValueType.Float) < 0 ||
                    ResolveParameter("trails.color", (int)FluxParticleValueType.Float4) < 0 ||
                    ResolveParameter("trails.duration", (int)FluxParticleValueType.Float) < 0) return false;
                var duration = GetParameterValue(ResolveParameter("trails.duration", (int)FluxParticleValueType.Float)).x;
                if (!FluxParticleTrailHistory.IsValidDuration(duration, trailBuffers.Length - 2)) return false;
                for (var i = 0; i < trailBuffers.Length; i++)
                {
                    if (trailBuffers[i] == null) return false;
                }
                for (var i = 0; i < trailMaterials.Length; i++)
                {
                    if (trailMaterials[i] == null || trailRenderers[i] == null) return false;
                }
            }
            if (emitterRowProperties != null && emitterRowProperties.Length != 0)
            {
                if (emitterRowProperties.Length != 3 || emitterTransform == null) return false;
                for (var row = 0; row < 3; row++)
                {
                    if (string.IsNullOrEmpty(emitterRowProperties[row])) return false;
                }
            }
            if (allocationBuffers == null || allocationBuffers.Length < 1 || allocationBuffers.Length > 13 ||
                allocationKernels == null || allocationKernels.Length != allocationBuffers.Length + 2 || allocationLookupProperties == null) return false;
            for (var i = 0; i < allocationBuffers.Length; i++)
            {
                if (allocationBuffers[i] == null) return false;
            }
            for (var i = 0; i < allocationKernels.Length; i++)
            {
                if (allocationKernels[i] == null) return false;
            }
            if (stepKernels == null || clearKernels == null || clearKernels.Length == 0 || renderMaterial == null) return false;
            if (spawnMaskBuffer == null || spawnKeyBuffer == null) return false;
            if (pageBuffersA == null || pageBuffersB == null || pageBuffersA.Length != clearKernels.Length || pageBuffersB.Length != clearKernels.Length) return false;
            if (outputPages == null || spawnOnly == null || readOffsets == null || outputPages.Length != stepKernels.Length ||
                spawnOnly.Length != stepKernels.Length || readOffsets.Length != stepKernels.Length + 1) return false;
            if (readPages == null || readUpdated == null || readProperties == null || readUpdated.Length != readPages.Length || readProperties.Length != readPages.Length) return false;
            if (readOffsets[0] != 0 || readOffsets[stepKernels.Length] != readPages.Length) return false;
            if (renderPageProperties == null || renderPageProperties.Length != clearKernels.Length) return false;
            if (parameterIds == null || parameterProperties == null || parameterTypes == null || staticParameters == null || parameterValues == null) return false;
            if (parameterIds.Length != parameterProperties.Length || parameterIds.Length != parameterTypes.Length ||
                parameterIds.Length != staticParameters.Length || parameterIds.Length != parameterValues.Length) return false;
            if (parameterConstraintFlags == null || parameterMinimum == null || parameterMaximum == null ||
                parameterConstraintFlags.Length != parameterIds.Length || parameterMinimum.Length != parameterIds.Length ||
                parameterMaximum.Length != parameterIds.Length) return false;
            if (string.IsNullOrEmpty(deltaTimeProperty) || string.IsNullOrEmpty(spawnKeyProperty)) return false;

            for (var i = 0; i < stepKernels.Length; i++)
            {
                if (stepKernels[i] == null || outputPages[i] < 0 || outputPages[i] >= clearKernels.Length ||
                    readOffsets[i] < 0 || readOffsets[i] > readOffsets[i + 1]) return false;
            }
            for (var i = 0; i < clearKernels.Length; i++)
            {
                if (clearKernels[i] == null || pageBuffersA[i] == null || pageBuffersB[i] == null ||
                    pageBuffersA[i] == pageBuffersB[i] || string.IsNullOrEmpty(renderPageProperties[i])) return false;
            }
            for (var i = 0; i < readPages.Length; i++)
            {
                if (readPages[i] < 0 || readPages[i] >= clearKernels.Length || string.IsNullOrEmpty(readProperties[i])) return false;
            }
            for (var i = 0; i < parameterIds.Length; i++)
            {
                if (string.IsNullOrEmpty(parameterIds[i]) || string.IsNullOrEmpty(parameterProperties[i]) ||
                    parameterTypes[i] < 1 || parameterTypes[i] > 4) return false;
            }
            return true;
        }
    }
}
