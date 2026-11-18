using System;
using AF.Core.Editor;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WrongLine.Crash;

namespace WrongLine.Driving.Editor
{
    /// <summary>
    /// The camera is a rig object, a Cinemachine brain and two virtual cameras that all have to be wired right, or the picture is
    /// wrong in ways that are hard to see (a damped body brings back the frame-time-dependent lag that made the car jitter; a
    /// brain that updates at another time lags the camera a frame behind the car). This fails the validation instead.
    /// </summary>
    public sealed class ChaseCameraSetupValidator : IValidator
    {
        private const string GAME_FOLDER = "Assets/Game";

        /// <summary>The wreck must stay this far inside the picture: half the field of view over the look limit, with room to spare.</summary>
        private const float FovMargin = 1.15f;

        public void ValidateAssets(ValidationReport report)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(ChaseCameraConfig)}", new[] { GAME_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<ChaseCameraConfig>(path);
                if (config == null)
                {
                    continue;
                }

                try
                {
                    config.Settings.Validate();
                }
                catch (ArgumentException exception)
                {
                    report.Error($"{path}: {exception.Message}", config);
                    continue;
                }

                foreach (string crashGuid in AssetDatabase.FindAssets($"t:{nameof(CrashConfig)}", new[] { GAME_FOLDER }))
                {
                    string crashPath = AssetDatabase.GUIDToAssetPath(crashGuid);
                    var crash = AssetDatabase.LoadAssetAtPath<CrashConfig>(crashPath);
                    CheckFov(config.Settings, crash.Settings.Camera.CrashFov, $"{crashPath} CrashFov", config, report);
                }

                CheckFov(config.Settings, config.Settings.BaseFov, $"{path} BaseFov", config, report);
            }
        }

        public void ValidateScene(Scene scene, ValidationReport report)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ChaseCameraView view in root.GetComponentsInChildren<ChaseCameraView>(true))
                {
                    CheckView(view, scene.path, report);
                }
            }
        }

        internal static void CheckFov(ChaseCameraSettings settings, float fieldOfView, string what, UnityEngine.Object context, ValidationReport report)
        {
            if (fieldOfView * 0.5f < settings.MaxCarPitchDegrees * FovMargin)
            {
                report.Error($"{what} is {fieldOfView}°: half of it must be over the camera's look limit ({settings.MaxCarPitchDegrees}° with {FovMargin:F2}x room), or the car leaves the picture.", context);
            }
        }

        private static void CheckView(ChaseCameraView view, string scenePath, ValidationReport report)
        {
            var so = new SerializedObject(view);
            var brain = so.FindProperty("_brain").objectReferenceValue as CinemachineBrain;
            var chase = so.FindProperty("_chase").objectReferenceValue as CinemachineCamera;
            var crash = so.FindProperty("_crash").objectReferenceValue as CinemachineCamera;
            var rig = so.FindProperty("_rig").objectReferenceValue as Transform;
            var shake = so.FindProperty("_shake").objectReferenceValue as Transform;

            if (brain == null || chase == null || crash == null || rig == null || shake == null)
            {
                report.Error($"{scenePath}: the camera view is missing its brain, virtual cameras, rig or shake object.", view);
                return;
            }

            if (shake.parent != rig)
            {
                report.Error($"{scenePath}: the shake object must be a child of the camera rig (it shakes in the rig's own space).", shake);
            }

            if (brain.UpdateMethod != CinemachineBrain.UpdateMethods.LateUpdate)
            {
                report.Error($"{scenePath}: the brain must update in LateUpdate (after the car and the rig have been placed, before the world drivers read the camera).", brain);
            }

            foreach (CinemachineCamera cm in new[] { chase, crash })
            {
                if (cm.Follow != shake)
                {
                    report.Error($"{scenePath}: {cm.name} must follow the shake object under the camera rig.", cm);
                }

                var position = cm.GetComponent<CinemachineHardLockToTarget>();
                var rotation = cm.GetComponent<CinemachineRotateWithFollowTarget>();
                if (position == null || rotation == null)
                {
                    report.Error($"{scenePath}: {cm.name} needs a Hard Lock To Target (position) and a Rotate With Follow Target (rotation).", cm);
                }
                else if (position.Damping != 0f || rotation.Damping != 0f)
                {
                    report.Error($"{scenePath}: {cm.name} must not damp its body or aim: damping brings back a lag that depends on the frame time (the car would jitter).", cm);
                }
            }

            if (chase.Priority.Value <= crash.Priority.Value)
            {
                report.Error($"{scenePath}: the chase camera must have a higher priority than the crash camera (it takes over at a crash).", chase);
            }
        }
    }
}
