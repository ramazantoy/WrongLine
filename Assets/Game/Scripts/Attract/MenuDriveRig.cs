using AF.Core;
using UnityEngine;
using UnityEngine.Rendering;
using WrongLine.Car;
using WrongLine.Crash;
using WrongLine.Driving;
using WrongLine.World;

namespace WrongLine.Attract
{
    /// <summary>
    /// The car, the camera rig, the sun and the night's bloom the menu's background drive is made of, with the configs it runs on, as
    /// one prefab. Not a copy of the game's scene: the same classes, an autopilot at the wheel and no crash. The car is not drawn
    /// (its renderers are off in the prefab): the camera rides in it, so a lane change tilts and turns the picture.
    /// </summary>
    public sealed class MenuDriveRig : MonoBehaviour
    {
        [SerializeField, Required] private CarConfig _carConfig;
        [SerializeField, Required] private CrashConfig _crashConfig;
        [SerializeField, Required] private ChaseCameraConfig _cameraConfig;
        [SerializeField, Required] private CurveWorldConfig _curveConfig;
        [SerializeField, Required] private DayNightConfig _dayNightConfig;
        [SerializeField, Required] private LightsConfig _lightsConfig;
        [SerializeField, Required] private AutopilotConfig _autopilotConfig;
        [SerializeField, Required] private FirstPersonCameraConfig _firstPersonConfig;
        [SerializeField, Required] private Volume _nightBloom;
        [SerializeField, Required] private Light _sun;
        [SerializeField, Required] private CarView _car;
        [SerializeField, Required] private ChaseCameraView _camera;

        internal DriveWorldParts Parts => new DriveWorldParts(_carConfig, _crashConfig, _cameraConfig, _curveConfig, _dayNightConfig, _lightsConfig, _nightBloom, _sun, _car, _camera);

        internal AutopilotConfig Autopilot => _autopilotConfig;

        internal FirstPersonCameraConfig FirstPerson => _firstPersonConfig;
    }
}
