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
        FluxKernel spawnMaskKernel;
        [SerializeField]
        FluxKernel spawnKeyKernel;
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
        string[] parameterProperties;
        [SerializeField]
        int[] parameterTypes;
        [SerializeField]
        bool[] staticParameters;
        [SerializeField]
        Vector4[] parameterValues;
        [SerializeField]
        string deltaTimeProperty;
        [SerializeField]
        string spawnKeyProperty;

        FluxBuffer[] _current;
        FluxBuffer[] _next;
        bool[] _updated;
        Material _render;
        int _count;
        bool _initialized;
        bool _started;

        /// <summary>
        /// このParticle System専用の描画Materialを取得します。
        /// </summary>
        public Material RenderMaterial => _render;

        /// <summary>
        /// FluxBufferを初期化し、確定済みManifestのGPU実行環境を構築します。
        /// </summary>
        /// <param name="count">有効Particle数。</param>
        /// <returns>初期化に成功した場合はtrue。</returns>
        public bool Initialize(int count)
        {
            if (_initialized) return true;
            if (count < 1 || count > 1048576 || !ValidateManifest())
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

            _render = new Material(renderMaterial);
            _render.SetVector("_FluxStateSize", stateSize);
            spawnMaskKernel.SetVector("_FluxStateSize", stateSize);
            spawnKeyKernel.SetVector("_FluxStateSize", stateSize);
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
        /// 全Pageの両BufferとSpawnMaskを初期状態に戻します。Static Parameterのロックは解除しません。
        /// </summary>
        public void Clear()
        {
            if (!_initialized) return;

            for (var page = 0; page < _current.Length; page++)
            {
                clearKernels[page].Dispatch(_current[page]);
                clearKernels[page].Dispatch(_next[page]);
            }
            GenerateSpawnInputs(0, 0, 0);
            BindRender();
        }

        /// <summary>
        /// Spawn入力をGPUで生成し、Compilerが確定した順序でGPU Passを実行します。
        /// </summary>
        /// <param name="deltaTime">Simulationの経過時間。</param>
        /// <param name="spawnStart">生成範囲の先頭Slot。</param>
        /// <param name="spawnCount">生成数。容量超過時は各Slotの最後の生成を採用します。</param>
        /// <param name="seedStart">20bit SpawnKeyの先頭。</param>
        public void Step(float deltaTime, int spawnStart, int spawnCount, int seedStart)
        {
            if (!_initialized) return;
            if (deltaTime < 0 || spawnStart < 0 || spawnStart >= _count || spawnCount < 0 || seedStart < 0 || seedStart >= 1048576) return;

            _started = true;
            GenerateSpawnInputs(spawnStart, spawnCount, seedStart);
            for (var page = 0; page < _updated.Length; page++)
            {
                _updated[page] = false;
            }

            for (var pass = 0; pass < stepKernels.Length; pass++)
            {
                if (spawnOnly[pass] && spawnCount == 0) continue;

                var kernel = stepKernels[pass];
                kernel.SetFloat(deltaTimeProperty, deltaTime);
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
            BindRender();
        }

        /// <summary>
        /// 論理IDでFloat Parameterを変更します。
        /// </summary>
        /// <param name="id">Parameter ID。</param>
        /// <param name="value">設定値。</param>
        /// <returns>型とStatic制約を満たして設定できた場合はtrue。</returns>
        public bool SetFloat(string id, float value)
        {
            var index = FindParameter(id);
            if (index < 0 || parameterTypes[index] != 1 || (_started && staticParameters[index])) return false;
            ApplyParameter(index, new Vector4(value, 0, 0, 0));
            return true;
        }

        /// <summary>
        /// 論理IDでFloat2/3/4 Parameterを変更します。
        /// </summary>
        /// <param name="id">Parameter ID。</param>
        /// <param name="value">設定するVector。</param>
        /// <returns>型とStatic制約を満たして設定できた場合はtrue。</returns>
        public bool SetVector(string id, Vector4 value)
        {
            var index = FindParameter(id);
            if (index < 0 || parameterTypes[index] < 2 || parameterTypes[index] > 4 || (_started && staticParameters[index])) return false;
            ApplyParameter(index, value);
            return true;
        }

        /// <summary>
        /// 確定済みPageのRenderTextureを取得します。Stepをまたいで参照を保持しないでください。
        /// </summary>
        /// <param name="page">Compilerが確定したPage ID。</param>
        /// <returns>該当PageのTexture。範囲外ならnull。</returns>
        public RenderTexture GetCurrentPage(int page)
        {
            if (!_initialized || page < 0 || page >= _current.Length) return null;
            return _current[page].Texture;
        }

        /// <summary>
        /// このInstanceが所有する描画Materialを解放します。
        /// FluxKernelとFluxBufferのリソース所有権はFlux Coreにあります。
        /// </summary>
        public void Release()
        {
            _initialized = false;
            if (_render != null)
            {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                if (!Application.isPlaying)
                {
                    DestroyImmediate(_render);
                }
                else
#endif
                {
                    Destroy(_render);
                }
            }
            _render = null;
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
            buffer.SetCount(count);
            return buffer.Count == count;
        }

        void GenerateSpawnInputs(int start, int count, int seed)
        {
            var active = Mathf.Min(count, _count);
            var skipped = count - active;
            var first = (start + skipped % _count) % _count;
            var seedBase = (seed + skipped % 1048576) % 1048576;

            spawnMaskKernel.SetFloat("_FluxSpawnFirst", first);
            spawnMaskKernel.SetFloat("_FluxSpawnCount", active);
            spawnMaskKernel.Dispatch(spawnMaskBuffer);

            if (active == 0) return;
            spawnKeyKernel.SetFloat("_FluxSpawnFirst", first);
            spawnKeyKernel.SetFloat("_FluxSpawnCount", active);
            spawnKeyKernel.SetFloat("_FluxSpawnSeedBase", seedBase);
            spawnKeyKernel.Dispatch(spawnKeyBuffer);
        }

        int FindParameter(string id)
        {
            if (!_initialized) return -1;
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
            if (parameterTypes[index] == 1) _render.SetFloat(property, value.x);
            else _render.SetVector(property, value);
        }

        void BindRender()
        {
            for (var page = 0; page < _current.Length; page++)
            {
                _render.SetTexture(renderPageProperties[page], _current[page].Texture);
            }
        }

        bool ValidateManifest()
        {
            if (stepKernels == null || clearKernels == null || clearKernels.Length == 0 || renderMaterial == null) return false;
            if (spawnMaskKernel == null || spawnKeyKernel == null || spawnMaskBuffer == null || spawnKeyBuffer == null) return false;
            if (pageBuffersA == null || pageBuffersB == null || pageBuffersA.Length != clearKernels.Length || pageBuffersB.Length != clearKernels.Length) return false;
            if (outputPages == null || spawnOnly == null || readOffsets == null || outputPages.Length != stepKernels.Length ||
                spawnOnly.Length != stepKernels.Length || readOffsets.Length != stepKernels.Length + 1) return false;
            if (readPages == null || readUpdated == null || readProperties == null || readUpdated.Length != readPages.Length || readProperties.Length != readPages.Length) return false;
            if (readOffsets[0] != 0 || readOffsets[stepKernels.Length] != readPages.Length) return false;
            if (renderPageProperties == null || renderPageProperties.Length != clearKernels.Length) return false;
            if (parameterIds == null || parameterProperties == null || parameterTypes == null || staticParameters == null || parameterValues == null) return false;
            if (parameterIds.Length != parameterProperties.Length || parameterIds.Length != parameterTypes.Length ||
                parameterIds.Length != staticParameters.Length || parameterIds.Length != parameterValues.Length) return false;
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
