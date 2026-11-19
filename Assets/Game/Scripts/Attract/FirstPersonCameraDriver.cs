using UnityEngine;
using VContainer.Unity;
using WrongLine.Car;
using WrongLine.Driving;

namespace WrongLine.Attract
{
    /// <summary>
    /// Each frame, after the car has been placed, puts the camera rig in the car's driver's seat (<see cref="FirstPersonCameraRule"/>);
    /// Cinemachine's brain then moves the camera to it, and the world drivers that read the camera run after that. The car itself is
    /// not drawn: only what it does to the camera shows.
    /// </summary>
    internal sealed class FirstPersonCameraDriver : ILateTickable
    {
        private readonly ChaseCameraView _camera;
        private readonly CarView _car;
        private readonly ICarMotion _motion;
        private readonly FirstPersonCameraRule _rule;

        public FirstPersonCameraDriver(ChaseCameraView camera, CarView car, ICarMotion motion, FirstPersonCameraConfig config)
        {
            _camera = camera;
            _car = car;
            _motion = motion;
            // The rule reads the config's settings object every frame, so tuning it in the Inspector shows immediately.
            _rule = new FirstPersonCameraRule(config.Settings);
        }

        public void LateTick()
        {
            _car.BodyPose(out Vector3 position, out Quaternion rotation);
            _camera.Apply(_rule.Position(position, rotation), _rule.Rotation(rotation), _rule.Fov(_motion.Speed));
        }
    }
}
