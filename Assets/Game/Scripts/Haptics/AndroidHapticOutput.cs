#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

namespace WrongLine.Haptics
{
    /// <summary>
    /// The Android vibrator. API 26+ takes a strength (<c>VibrationEffect.createOneShot</c>); older devices get the fixed pulse of
    /// <see cref="Handheld.Vibrate"/>, whose use here also makes Unity add the VIBRATE permission to the manifest.
    /// </summary>
    public sealed class AndroidHapticOutput : IHapticOutput
    {
        private const int OneShotApi = 26;

        private readonly AndroidJavaObject _vibrator;
        private readonly int _api;

        public AndroidHapticOutput()
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            _api = version.GetStatic<int>("SDK_INT");
        }

        public void Vibrate(int milliseconds, int amplitude)
        {
            if (_vibrator == null)
            {
                return;
            }

            if (_api >= OneShotApi)
            {
                using var effects = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, Mathf.Clamp(amplitude, 1, 255));
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                Handheld.Vibrate();
            }
        }
    }
}
#endif
