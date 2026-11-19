using AF.Core;
using UnityEngine;

namespace WrongLine.Audio
{
    /// <summary>
    /// The car prefab's handle on its sound: the config with the numbers and the cues. The loops play through <c>ISounds</c>
    /// and go on while the car is not drawn (the menu's drive).
    /// </summary>
    public sealed class CarAudioView : MonoBehaviour
    {
        [SerializeField, Required] private CarAudioConfig _config;

        public CarAudioConfig Config => _config;
    }
}
