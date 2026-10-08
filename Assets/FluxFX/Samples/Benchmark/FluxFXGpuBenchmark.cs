
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace TpLab.Flux.FX.Samples
{
    public class FluxFXGpuBenchmark : MonoBehaviour
    {
        [SerializeField]
        string caseId = "G01";

        [SerializeField]
        [Min(0)]
        float warmupSeconds = 20f;

        [SerializeField]
        [Min(1)]
        float measureSeconds = 10f;

        const int TimingDelayFrames = 8;

        readonly FrameTiming[] _timings = new FrameTiming[1];
        readonly List<double> _gpuSamples = new List<double>();
        readonly List<double> _cpuSamples = new List<double>();
        readonly List<string> _sampleRows = new List<string>();

        float _elapsedTime;
        int _delayFrames;
        int _invalidGpuSamples;
        bool _measuring;
        bool _finished;
        ulong _lastFrameStartTimestamp;
        int _duplicateSamples;

        #region Unity Events

        void Start()
        {
            Debug.Log($"[FluxFX Benchmark] {caseId}: Warmup started.");
        }

        void Update()
        {
            if (_finished) return;

            _elapsedTime += Time.unscaledDeltaTime;

            if (!_measuring)
            {
                if (_elapsedTime < warmupSeconds) return;

                _measuring = true;
                _elapsedTime = 0;
                _delayFrames = TimingDelayFrames;

                Debug.Log($"[FluxFX Benchmark] {caseId}: Measurement started.");
                return;
            }

            if (_elapsedTime >= measureSeconds)
            {
                Finish();
                return;
            }

            if (_delayFrames > 0)
            {
                _delayFrames--;
                return;
            }

            CollectSample();
        }

        void LateUpdate()
        {
            if (_finished) return;

            FrameTimingManager.CaptureFrameTimings();
        }

        #endregion

        #region Private Methods

        void CollectSample()
        {
            var count = FrameTimingManager.GetLatestTimings(1, _timings);

            if (count == 0)
            {
                _invalidGpuSamples++;
                return;
            }

            var timing = _timings[0];

            if (timing.frameStartTimestamp == 0)
            {
                _invalidGpuSamples++;
                return;
            }

            if (timing.frameStartTimestamp <= _lastFrameStartTimestamp)
            {
                _duplicateSamples++;
                return;
            }

            _lastFrameStartTimestamp = timing.frameStartTimestamp;

            var gpuMs = timing.gpuFrameTime;
            var cpuMs = timing.cpuFrameTime;

            if (!IsValid(gpuMs))
            {
                _invalidGpuSamples++;
                return;
            }

            _gpuSamples.Add(gpuMs);

            var validCpu = IsValid(cpuMs);

            if (validCpu)
            {
                _cpuSamples.Add(cpuMs);
            }

            var cpuText = validCpu ? Format(cpuMs) : "";

            _sampleRows.Add(
                $"{_gpuSamples.Count},{Format(gpuMs)},{cpuText}");
        }

        void Finish()
        {
            _finished = true;

            if (_gpuSamples.Count == 0)
            {
                Debug.LogError(
                    $"[FluxFX Benchmark] {caseId}: No valid GPU timings. " +
                    "Check Player Settings > Frame Timing Stats.");
                return;
            }

            var gpuAverage = Average(_gpuSamples);
            var gpuMedian = Percentile(_gpuSamples, 0.5);
            var gpuP95 = Percentile(_gpuSamples, 0.95);

            var cpuAverage = Average(_cpuSamples);
            var cpuMedian = Percentile(_cpuSamples, 0.5);
            var cpuP95 = Percentile(_cpuSamples, 0.95);

            var directory = Path.Combine(
                Application.persistentDataPath, "FluxFXBenchmarks");

            Directory.CreateDirectory(directory);

            var timestamp = DateTime.Now.ToString(
                "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            var filePrefix = $"{caseId}_{timestamp}";

            var summaryPath = Path.Combine(directory, filePrefix + "_summary.csv");
            var samplesPath = Path.Combine(directory, filePrefix + "_samples.csv");

            var summary = new StringBuilder();

            summary.AppendLine(
                "Case,GPUCount,CPUCount,InvalidGPU,GPUAvgMs,GPUMedianMs,GPUP95Ms,CPUAvgMs,CPUMedianMs,CPUP95Ms");

            summary.AppendLine(
                $"{caseId},{_gpuSamples.Count},{_cpuSamples.Count},{_invalidGpuSamples}," +
                $"{Format(gpuAverage)},{Format(gpuMedian)},{Format(gpuP95)}," +
                $"{Format(cpuAverage)},{Format(cpuMedian)},{Format(cpuP95)}");

            var samples = new StringBuilder();

            samples.AppendLine("Sample,GPUms,CPUms");

            foreach (var row in _sampleRows)
            {
                samples.AppendLine(row);
            }

            // File.WriteAllText(summaryPath, summary.ToString());
            // File.WriteAllText(samplesPath, samples.ToString());

            Debug.Log(
                $"[FluxFX Benchmark] {caseId} completed.\n" +
                $"GPU: Avg={Format(gpuAverage)} ms, " +
                $"Median={Format(gpuMedian)} ms, " +
                $"P95={Format(gpuP95)} ms\n" +
                $"Valid GPU samples: {_gpuSamples.Count}, " +
                $"Invalid: {_invalidGpuSamples}, " +
                $"Duplicate: {_duplicateSamples}\n" +
                $"CSV: {summaryPath}");
        }

        static bool IsValid(double value)
        {
            return value > 0 &&
                   !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }

        static double Average(List<double> values)
        {
            if (values.Count == 0) return double.NaN;

            double total = 0;

            foreach (var value in values)
            {
                total += value;
            }

            return total / values.Count;
        }

        static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0) return double.NaN;

            var sorted = new List<double>(values);
            sorted.Sort();

            var position = (sorted.Count - 1) * percentile;
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);

            if (lower == upper) return sorted[lower];

            var fraction = position - lower;

            return sorted[lower] +
                   (sorted[upper] - sorted[lower]) * fraction;
        }

        static string Format(double value)
        {
            return value.ToString("F4", CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
