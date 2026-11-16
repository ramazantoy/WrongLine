using UnityEngine;
using VContainer.Unity;
using WrongLine.Driving;
using WrongLine.Road;

namespace WrongLine.World
{
    /// <summary>
    /// Tells every curved shader where the camera is and how hard to bend, once a frame after the camera has moved, and keeps
    /// the fog range in step with how far ahead the road is built. The shader values are globals, so no material is ever
    /// written to. See <c>CurveWorld.hlsl</c>. The fog colour belongs to <see cref="DayNightDriver"/>.
    /// </summary>
    internal sealed class CurveWorldDriver : IStartable, IPostLateTickable
    {
        private static readonly int _originId = Shader.PropertyToID("_WL_CurveOrigin");
        private static readonly int _paramsId = Shader.PropertyToID("_WL_CurveParams");

        private readonly ChaseCameraView _camera;
        private readonly CurveWorldConfig _config;
        private readonly RoadConfig _road;

        public CurveWorldDriver(ChaseCameraView camera, CurveWorldConfig config, RoadConfig road)
        {
            config.Settings.Validate();
            _camera = camera;
            _config = config;
            _road = road;
        }

        public void Start()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            ApplyFog(); // Throws on a fog that cannot fit, here at the start of the run rather than as a wrong picture.
        }

        public void PostLateTick()
        {
            CurveWorldSettings settings = _config.Settings;
            Vector3 position = _camera.Position;
            Shader.SetGlobalVector(_originId, new Vector4(position.x, position.y, position.z, 0f));
            Shader.SetGlobalVector(_paramsId, new Vector4(settings.StartMeters, settings.Strength, 0f, 0f));
            ApplyFog();
        }

        private void ApplyFog()
        {
            (float start, float end) = FogRange.Compute(_config.Settings, _road.AheadMeters);
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = end;
        }
    }
}
