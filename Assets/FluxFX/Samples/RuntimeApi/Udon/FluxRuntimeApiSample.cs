using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace FluxFX.Samples.RuntimeApi.Udon
{
    public enum SampleControl
    {
        Lifetime = 0,
        Speed = 1,
        StartSize = 2,
        StartR = 3,
        StartG = 4,
        StartB = 5,
        StartA = 6,
        StartRotation = 7,
        Rate = 8,
        Duration = 9,
        Delay = 10,
        BurstTime = 11,
        BurstCount = 12,
        PositionX = 13,
        PositionY = 14,
        PositionZ = 15,
        Radius = 16,
        Angle = 17,
        BoxX = 18,
        BoxY = 19,
        BoxZ = 20,
        GravityX = 21,
        GravityY = 22,
        GravityZ = 23,
        Drag = 24,
        ForceX = 25,
        ForceY = 26,
        ForceZ = 27,
        NoiseStrength = 28,
        NoiseScale = 29,
        NoiseSpeed = 30,
        VortexStrength = 31,
        VortexAxisX = 32,
        VortexAxisY = 33,
        VortexAxisZ = 34,
        VortexCenterX = 35,
        VortexCenterY = 36,
        VortexCenterZ = 37,
        MaxSpeed = 38,
        VelocityStartX = 39,
        VelocityStartY = 40,
        VelocityStartZ = 41,
        VelocityEndX = 42,
        VelocityEndY = 43,
        VelocityEndZ = 44,
        OrbitalX = 45,
        OrbitalY = 46,
        OrbitalZ = 47,
        OrbitOffsetX = 48,
        OrbitOffsetY = 49,
        OrbitOffsetZ = 50,
        Radial = 51,
        LiftStrength = 52,
        LiftX = 53,
        LiftY = 54,
        LiftZ = 55,
        EndR = 56,
        EndG = 57,
        EndB = 58,
        EndA = 59,
        EndSize = 60,
        EndRotation = 61,
        Cycles = 62,
        TilesX = 63,
        TilesY = 64,
        Count = 65
    }

    /// <summary>
    /// UIの値をMain・Emission・ShapeとModuleのRuntime APIへ渡します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxRuntimeApiSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxParticleSystem[] systems;
        [SerializeField]
        Slider[] controls;
        [SerializeField]
        Text[] values;
        [SerializeField]
        Toggle[] moduleToggles;
        [SerializeField]
        Toggle loopToggle;
        [SerializeField]
        Dropdown shapeSelector;
        [SerializeField]
        GameObject[] pages;
        [SerializeField]
        Text status;
        [SerializeField]
        Text message;
        [SerializeField]
        Text playLabel;

        FluxParticleSystem _target;
        bool _ready;
        float _nextStatus;

        void Start()
        {
            SendCustomEventDelayedFrames(nameof(InitializeSample), 2);
        }

        public void InitializeSample()
        {
            for (var i = 0; i < systems.Length; i++)
            {
                systems[i].Pause();
            }
            _ready = true;
            OnShapeChanged();
            ShowMain();
        }

        void Update()
        {
            if (!_ready || Time.time < _nextStatus) return;
            _nextStatus = Time.time + 0.2f;
            status.text = _target.Shape.GetShapeType() + "  /  " + _target.Main.GetParticleCount() + " slots\n" +
                "Playback  " + _target.PlaybackTime.ToString("F1") + " s";
            playLabel.text = _target.IsPaused || !_target.IsEmitting ? "Play" : "Pause";
        }

        float Value(SampleControl control) => controls[(int)control].value;
        Vector3 Vector(SampleControl first) => new Vector3(controls[(int)first].value, controls[(int)first + 1].value, controls[(int)first + 2].value);
        Color Tint(SampleControl first) => new Color(controls[(int)first].value, controls[(int)first + 1].value, controls[(int)first + 2].value, controls[(int)first + 3].value);

        public void OnShapeChanged()
        {
            if (!_ready) return;
            if (_target != null)
            {
                _target.Pause();
                _target.Clear();
            }
            _target = systems[shapeSelector.value];
            controls[(int)SampleControl.Radius].interactable = _target.Shape.HasRadius();
            controls[(int)SampleControl.Angle].interactable = _target.Shape.HasAngle();
            controls[(int)SampleControl.BoxX].interactable = _target.Shape.HasSize();
            controls[(int)SampleControl.BoxY].interactable = _target.Shape.HasSize();
            controls[(int)SampleControl.BoxZ].interactable = _target.Shape.HasSize();
            ApplyInitialValues();
        }

        public void OnControlsChanged()
        {
            for (var i = 0; i < controls.Length; i++)
            {
                values[i].text = controls[i].value.ToString(controls[i].wholeNumbers ? "F0" : "F2");
            }
            if (!_ready) return;
            ApplyLiveValues();
            message.text = "Live values applied. Initial values need Apply & Restart.";
        }

        void ApplyLiveValues()
        {
            _target.Main.SetStartSpeed(Value(SampleControl.Speed));
            _target.Main.SetDuration(Value(SampleControl.Duration));
            _target.Main.SetStartDelay(Value(SampleControl.Delay));
            _target.Main.SetLoop(loopToggle.isOn);
            _target.Emission.SetRate(Value(SampleControl.Rate));
            _target.Shape.SetPosition(Vector(SampleControl.PositionX));
            if (_target.Shape.HasRadius())
            {
                _target.Shape.SetRadius(Value(SampleControl.Radius));
            }
            if (_target.Shape.HasAngle())
            {
                _target.Shape.SetAngle(Value(SampleControl.Angle));
            }
            if (_target.Shape.HasSize())
            {
                _target.Shape.SetSize(Vector(SampleControl.BoxX));
            }
            var modules = _target.Modules;
            modules.Gravity.SetGravity(Vector(SampleControl.GravityX));
            modules.Drag.SetDrag(Value(SampleControl.Drag));
            modules.Force.SetForce(Vector(SampleControl.ForceX));
            modules.Noise.SetStrength(Value(SampleControl.NoiseStrength));
            modules.Noise.SetScale(Value(SampleControl.NoiseScale));
            modules.Noise.SetSpeed(Value(SampleControl.NoiseSpeed));
            modules.Vortex.SetStrength(Value(SampleControl.VortexStrength));
            modules.Vortex.SetAxis(Vector(SampleControl.VortexAxisX));
            modules.Vortex.SetCenter(Vector(SampleControl.VortexCenterX));
            modules.LimitVelocity.SetMaxSpeed(Value(SampleControl.MaxSpeed));
            modules.VelocityOverLifetime.SetStart(Vector(SampleControl.VelocityStartX));
            modules.VelocityOverLifetime.SetEnd(Vector(SampleControl.VelocityEndX));
            modules.VelocityOverLifetime.SetOrbital(Vector(SampleControl.OrbitalX));
            modules.VelocityOverLifetime.SetOffset(Vector(SampleControl.OrbitOffsetX));
            modules.VelocityOverLifetime.SetRadial(Value(SampleControl.Radial));
            modules.ColorOverLifetime.SetEndColor(Tint(SampleControl.EndR));
            modules.SizeOverLifetime.SetEndSize(Value(SampleControl.EndSize));
            modules.RotationOverLifetime.SetEndRotation(new Vector3(0, 0, Value(SampleControl.EndRotation)));
            modules.TextureSheetAnimation.SetCycles(Value(SampleControl.Cycles));
            // modules.CustomLift().SetStrength(Value(SampleControl.LiftStrength));
            // modules.CustomLift().SetDirection(Vector(SampleControl.LiftX));
            modules.Gravity.SetEnabled(moduleToggles[0].isOn);
            modules.Drag.SetEnabled(moduleToggles[1].isOn);
            modules.Force.SetEnabled(moduleToggles[2].isOn);
            modules.Noise.SetEnabled(moduleToggles[3].isOn);
            modules.Vortex.SetEnabled(moduleToggles[4].isOn);
            modules.LimitVelocity.SetEnabled(moduleToggles[5].isOn);
            modules.VelocityOverLifetime.SetEnabled(moduleToggles[6].isOn);
            // modules.CustomLift().SetEnabled(moduleToggles[7].isOn);
        }

        void ApplyBursts()
        {
            var count = (int)Value(SampleControl.BurstCount);
            _target.Emission.SetBursts(count == 0 ? new float[0] : new[] { Value(SampleControl.BurstTime) },
                count == 0 ? new int[0] : new[] { count });
        }

        /// <summary>
        /// GPU実行環境を再初期化し、Static Parameterを設定して再生成します。
        /// </summary>
        public void ApplyInitialValues()
        {
            if (!_ready) return;
            _target.Pause();
            _target.Executor.Release();
            if (!_target.Executor.Initialize(_target.ParticleCount))
            {
                message.text = "Initialization failed. Check the Unity Console.";
                return;
            }
            var accepted = _target.Main.SetStartLifetime(Value(SampleControl.Lifetime));
            accepted &= _target.Main.SetStartSize(Value(SampleControl.StartSize));
            accepted &= _target.Main.SetStartColor(Tint(SampleControl.StartR));
            accepted &= _target.Main.SetStartRotation(new Vector3(0, 0, Value(SampleControl.StartRotation)));
            accepted &= _target.Modules.TextureSheetAnimation.SetTiles((int)Value(SampleControl.TilesX), (int)Value(SampleControl.TilesY));
            ApplyLiveValues();
            ApplyBursts();
            _target.Restart();
            OnControlsChanged();
            message.text = accepted ? "Initial values applied. Particles restarted." : "Some initial values were rejected.";
        }

        public void TogglePlay()
        {
            if (!_ready) return;
            if (_target.IsPaused || !_target.IsEmitting)
            {
                _target.Play();
            }
            else
            {
                _target.Pause();
            }
        }

        public void Stop()
        {
            if (!_ready) return;
            _target.Stop();
            message.text = "Emission stopped. Existing particles continue.";
        }

        public void Restart()
        {
            if (!_ready) return;
            ApplyLiveValues();
            ApplyBursts();
            _target.Clear();
            _target.Restart();
            message.text = "Playback settings applied. Particles restarted.";
        }

        public void ShowMain() => ShowPage(0);
        public void ShowEmission() => ShowPage(1);
        public void ShowShape() => ShowPage(2);
        public void ShowMotion() => ShowPage(3);
        public void ShowRender() => ShowPage(4);

        void ShowPage(int index)
        {
            for (var i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == index);
            }
        }
    }
}

