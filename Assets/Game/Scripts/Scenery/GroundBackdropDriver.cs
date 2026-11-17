using System;
using AF.Core;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer.Unity;
using WrongLine.Driving;
using WrongLine.World;
using Object = UnityEngine.Object;

namespace WrongLine.Scenery
{
    /// <summary>
    /// Keeps a wide flat disc of ground under the camera, a little below the lowest road piece in the scene
    /// (<see cref="GroundBackdropRule"/>): where the ground strips end or leave a gap, this shows instead of the sky. It is
    /// made by this driver and destroyed with the scene's scope; it follows the camera once a frame after the camera moved.
    /// </summary>
    internal sealed class GroundBackdropDriver : IStartable, IPostLateTickable, IDisposable
    {
        private const int Rings = 10;
        private const int Segments = 32;

        private readonly ChaseCameraView _camera;
        private readonly IRoadFrame _road;
        private readonly SceneryConfig _config;
        private readonly CurveWorldConfig _curve;
        private readonly IGameClock _clock;
        private GameObject _disc;
        private Mesh _mesh;
        private double _height;

        public GroundBackdropDriver(ChaseCameraView camera, IRoadFrame road, SceneryConfig config, CurveWorldConfig curve, IGameClock clock)
        {
            _camera = camera;
            _road = road;
            _config = config;
            _curve = curve;
            _clock = clock;
        }

        public void Start()
        {
            if (_config.BackdropMaterial == null)
            {
                Log.Error("[Scenery] The scenery config has no backdrop material; the ground's gaps show the sky.");
                return;
            }

            _mesh = DiscMesh.Build(_config.BackdropRadius, Rings, Segments);
            _disc = new GameObject("GroundBackdrop");
            _disc.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = _disc.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _config.BackdropMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            CurvedBounds.Expand(renderer, _disc.GetComponent<MeshFilter>(), CurveBend.MaxDrop(_curve.Settings, _config.BackdropRadius));

            _height = Target();
            Place();
        }

        public void PostLateTick()
        {
            if (_disc == null)
            {
                return;
            }

            _height = GroundBackdropRule.Next(_height, Target(), _clock.DeltaTime, _config.BackdropRiseSpeed);
            Place();
        }

        public void Dispose()
        {
            if (_disc != null)
            {
                Object.Destroy(_disc);
            }

            if (_mesh != null)
            {
                Object.Destroy(_mesh);
            }
        }

        private double Target()
        {
            return GroundBackdropRule.Target(_road.Path, _road.FirstActive, _road.LastActive, _config.BackdropDepth);
        }

        private void Place()
        {
            Vector3 camera = _camera.Position;
            _disc.transform.position = new Vector3(camera.x, (float)(_height - _road.Origin.Y), camera.z);
        }
    }
}
