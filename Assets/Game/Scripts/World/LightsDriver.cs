using System;
using System.Collections.Generic;
using AF.Core;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer.Unity;
using WrongLine.Driving;
using WrongLine.Scenery;

namespace WrongLine.World
{
    /// <summary>
    /// Runs the night lights: each frame, after the day and night driver, it turns the darkness into the level of the street lamps
    /// and of the car's headlights (<see cref="LightSwitchRule"/>), writes them and the lamps' pools of light as shader globals
    /// (no material is written to), and weighs the night bloom volume. Street lamps are emission plus a fake pool of light on the
    /// road under the nearest few; the car's lights are real lights, driven by <see cref="WrongLine.Car.CarView"/> from the levels here.
    /// </summary>
    internal sealed class LightsDriver : IStartable, IPostLateTickable, ILightLevels
    {
        private static readonly int _levelsId = Shader.PropertyToID("_WL_LightLevels");
        private static readonly int _poolsId = Shader.PropertyToID("_WL_LampPools");
        private static readonly int _poolParamsId = Shader.PropertyToID("_WL_LampPoolParams");
        private static readonly int _poolColorId = Shader.PropertyToID("_WL_LampPoolColor");

        private readonly IGameClock _clock;
        private readonly LightsConfig _config;
        private readonly IDayNight _dayNight;
        private readonly IRoadsideLamps _lamps;
        private readonly ChaseCameraView _camera;
        private readonly Volume _nightBloom;
        private readonly LampPicker _picker = new LampPicker();
        private readonly List<Vector3> _heads = new List<Vector3>();
        private readonly Vector3[] _picked = new Vector3[LampPoolFalloff.MaxPools];
        private readonly Vector4[] _pools = new Vector4[LampPoolFalloff.MaxPools];
        private LightSwitchRule _streetLamps;
        private LightSwitchRule _headlights;

        public LightsDriver(LightsConfig config, IDayNight dayNight, IRoadsideLamps lamps, ChaseCameraView camera, Volume nightBloom, IGameClock clock)
        {
            _clock = clock;
            _config = config;
            _dayNight = dayNight;
            _lamps = lamps;
            _camera = camera;
            _nightBloom = nightBloom;
        }

        public float StreetLamps => _streetLamps?.Level ?? 0f;

        public float Headlights => _headlights?.Level ?? 0f;

        public void Start()
        {
            LightsSettings settings = _config.Settings;
            settings.Validate();
            _streetLamps = new LightSwitchRule(settings.StreetLamps);
            _headlights = new LightSwitchRule(settings.Headlights);
            Apply(0f);
        }

        public void PostLateTick()
        {
            Apply(_clock.DeltaTime);
        }

        private void Apply(float deltaTime)
        {
            LightsSettings settings = _config.Settings;
            float darkness = _dayNight.Darkness;
            _streetLamps.Update(darkness, deltaTime);
            _headlights.Update(darkness, deltaTime);

            Shader.SetGlobalVector(_levelsId, new Vector4(_streetLamps.Level, _headlights.Level, 0f, 0f));

            // The pools of light: the nearest few lamps, in the scene's flat space (the shader compares them with the unbent surface).
            _heads.Clear();
            IReadOnlyList<Transform> heads = _lamps.Heads;
            for (int i = 0; i < heads.Count; i++)
            {
                _heads.Add(heads[i].position);
            }

            int count = _streetLamps.Level > 0f
                ? _picker.Pick(_heads, _camera.Position, Math.Min(settings.LampPoolCount, LampPoolFalloff.MaxPools), _picked)
                : 0;
            for (int i = 0; i < _pools.Length; i++)
            {
                _pools[i] = i < count ? new Vector4(_picked[i].x, _picked[i].y, _picked[i].z, 1f) : Vector4.zero;
            }

            Shader.SetGlobalVectorArray(_poolsId, _pools);
            Shader.SetGlobalVector(_poolParamsId, new Vector4(settings.LampPoolRadius, settings.LampPoolVertical, settings.LampPoolStrength * _streetLamps.Level, count));
            Shader.SetGlobalColor(_poolColorId, settings.LampPoolColor);

            _nightBloom.weight = Mathf.Max(_streetLamps.Level, _headlights.Level) * settings.NightBloomWeight;
        }
    }
}
