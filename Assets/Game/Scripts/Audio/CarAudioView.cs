using AF.Core;
using UnityEngine;

namespace WrongLine.Audio
{
    /// <summary>
    /// The car's looping voices: two engine loops (low and high, crossfaded by the revs) and the road's rush, on three audio sources
    /// of the car prefab that play through the effects group. The loops go on while the car is not drawn (the menu's drive).
    /// </summary>
    public sealed class CarAudioView : MonoBehaviour
    {
        [SerializeField, Required] private CarAudioConfig _config;
        [SerializeField, Required] private AudioSource _engineLow;
        [SerializeField, Required] private AudioSource _engineHigh;
        [SerializeField, Required] private AudioSource _wind;

        public CarAudioConfig Config => _config;

        public void Apply(in EngineSound sound)
        {
            _engineLow.pitch = sound.Pitch;
            _engineHigh.pitch = sound.Pitch;
            _engineLow.volume = sound.LowVolume;
            _engineHigh.volume = sound.HighVolume;
            _wind.volume = sound.WindVolume;
            _wind.pitch = sound.WindPitch;
        }
    }
}
