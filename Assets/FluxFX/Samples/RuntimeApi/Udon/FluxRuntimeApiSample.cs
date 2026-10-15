using TpLab.Flux.FX.Udon;
using TpLab.Flux.FX.Udon.Modules;
using TpLab.Flux.FX.Udon.Parameters;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace FluxFX.Samples.RuntimeApi.Udon
{
    public enum SampleModule
    {
        Gravity,
        Drag,
        Force,
        Noise,
        Vortex,
        LimitVelocity,
        VelocityOverLifetime,
        ColorOverLifetime,
        SizeOverLifetime,
        RotationOverLifetime,
        TextureSheetAnimation,
        Count
    }

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
        EndR = 52,
        EndG = 53,
        EndB = 54,
        EndA = 55,
        EndSize = 56,
        EndRotation = 57,
        Cycles = 58,
        TilesX = 59,
        TilesY = 60,
        Count = 61
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
        CanvasGroup[] moduleParameterGroups;
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
            _target.RequestAliveCount();
            var alive = _target.AliveCount;
            status.text = shapeSelector.captionText.text + "  /  Alive " + (alive < 0 ? "--" : alive.ToString()) +
                " / " + _target.Main.GetParticleCount() + " slots\n" +
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
            RefreshModuleControls();
            for (var i = 0; i < controls.Length; i++)
            {
                values[i].text = controls[i].value.ToString(controls[i].wholeNumbers ? "F0" : "F2");
            }
            if (!_ready) return;
            ApplyLiveValues();
            message.text = "Live values applied. Initial values need Apply & Restart.";
        }

        public void RefreshModuleControls()
        {
            for (var i = 0; i < moduleParameterGroups.Length; i++)
            {
                var available = _target == null || GetModule((SampleModule)i) != null;
                moduleToggles[i].interactable = available;
                var enabled = available && moduleToggles[i].isOn;
                moduleParameterGroups[i].interactable = enabled;
                moduleParameterGroups[i].alpha = enabled ? 1f : 0.35f;
            }
        }

        FluxParticleModuleHandle GetModule(SampleModule module)
        {
            var modules = _target.Modules;
            switch (module)
            {
                case SampleModule.Gravity: return modules.Gravity;
                case SampleModule.Drag: return modules.Drag;
                case SampleModule.Force: return modules.Force;
                case SampleModule.Noise: return modules.Noise;
                case SampleModule.Vortex: return modules.Vortex;
                case SampleModule.LimitVelocity: return modules.LimitVelocity;
                case SampleModule.VelocityOverLifetime: return modules.VelocityOverLifetime;
                case SampleModule.ColorOverLifetime: return modules.ColorOverLifetime;
                case SampleModule.SizeOverLifetime: return modules.SizeOverLifetime;
                case SampleModule.RotationOverLifetime: return modules.RotationOverLifetime;
                case SampleModule.TextureSheetAnimation: return modules.TextureSheetAnimation;
                default: return null;
            }
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
            ApplyModuleValues();
        }

        void ApplyModuleValues()
        {
            var modules = _target.Modules;
            if (modules.Gravity != null)
            {
                modules.Gravity.SetGravity(Vector(SampleControl.GravityX));
                modules.Gravity.SetEnabled(moduleToggles[(int)SampleModule.Gravity].isOn);
            }
            if (modules.Drag != null)
            {
                modules.Drag.SetDrag(Value(SampleControl.Drag));
                modules.Drag.SetEnabled(moduleToggles[(int)SampleModule.Drag].isOn);
            }
            if (modules.Force != null)
            {
                modules.Force.SetForce(Vector(SampleControl.ForceX));
                modules.Force.SetEnabled(moduleToggles[(int)SampleModule.Force].isOn);
            }
            if (modules.Noise != null)
            {
                modules.Noise.SetStrength(Value(SampleControl.NoiseStrength));
                modules.Noise.SetScale(Value(SampleControl.NoiseScale));
                modules.Noise.SetSpeed(Value(SampleControl.NoiseSpeed));
                modules.Noise.SetEnabled(moduleToggles[(int)SampleModule.Noise].isOn);
            }
            if (modules.Vortex != null)
            {
                modules.Vortex.SetStrength(Value(SampleControl.VortexStrength));
                modules.Vortex.SetAxis(Vector(SampleControl.VortexAxisX));
                modules.Vortex.SetCenter(Vector(SampleControl.VortexCenterX));
                modules.Vortex.SetEnabled(moduleToggles[(int)SampleModule.Vortex].isOn);
            }
            if (modules.LimitVelocity != null)
            {
                modules.LimitVelocity.SetMaxSpeed(Value(SampleControl.MaxSpeed));
                modules.LimitVelocity.SetEnabled(moduleToggles[(int)SampleModule.LimitVelocity].isOn);
            }
            if (modules.VelocityOverLifetime != null)
            {
                modules.VelocityOverLifetime.SetStart(Vector(SampleControl.VelocityStartX));
                modules.VelocityOverLifetime.SetEnd(Vector(SampleControl.VelocityEndX));
                modules.VelocityOverLifetime.SetOrbital(Vector(SampleControl.OrbitalX));
                modules.VelocityOverLifetime.SetOffset(Vector(SampleControl.OrbitOffsetX));
                modules.VelocityOverLifetime.SetRadial(Value(SampleControl.Radial));
                modules.VelocityOverLifetime.SetEnabled(moduleToggles[(int)SampleModule.VelocityOverLifetime].isOn);
            }
            if (modules.ColorOverLifetime != null)
            {
                modules.ColorOverLifetime.SetEndColor(Tint(SampleControl.EndR));
                modules.ColorOverLifetime.SetEnabled(moduleToggles[(int)SampleModule.ColorOverLifetime].isOn);
            }
            if (modules.SizeOverLifetime != null)
            {
                modules.SizeOverLifetime.SetEndSize(Value(SampleControl.EndSize));
                modules.SizeOverLifetime.SetEnabled(moduleToggles[(int)SampleModule.SizeOverLifetime].isOn);
            }
            if (modules.RotationOverLifetime != null)
            {
                modules.RotationOverLifetime.SetEndRotation(new Vector3(0, 0, Value(SampleControl.EndRotation)));
                modules.RotationOverLifetime.SetEnabled(moduleToggles[(int)SampleModule.RotationOverLifetime].isOn);
            }
            if (modules.TextureSheetAnimation != null)
            {
                modules.TextureSheetAnimation.SetCycles(Value(SampleControl.Cycles));
                modules.TextureSheetAnimation.SetEnabled(moduleToggles[(int)SampleModule.TextureSheetAnimation].isOn);
            }
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
            if (_target.Modules.TextureSheetAnimation != null)
            {
                accepted &= _target.Modules.TextureSheetAnimation.SetTiles((int)Value(SampleControl.TilesX), (int)Value(SampleControl.TilesY));
            }
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

