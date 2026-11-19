using AF.Audio;
using AF.Core;
using UnityEngine;

namespace WrongLine.Audio
{
    /// <summary>The numbers of the car's sound: the engine and the whoosh of cars passing. Read every frame, so it can be tuned while the game runs.</summary>
    [System.Serializable]
    public sealed class CarAudioSettings
    {
        public EngineSoundSettings Engine = new EngineSoundSettings();
        public PassBySettings PassBy = new PassBySettings();

        public void Validate()
        {
            Engine.Validate();
            PassBy.Validate();
        }
    }

    /// <summary>
    /// The car's sound: the numbers and the cues of its loops (two engine loops and the road's rush) and of what happens around it
    /// (a car passing, the turn signal, a sideswipe, the crash).
    /// The menu's background drive has its own, quieter one and no crash.
    /// </summary>
    [CreateAssetMenu(menuName = "WrongLine/Car Audio Config")]
    public sealed class CarAudioConfig : ScriptableObject
    {
        [SerializeField] private CarAudioSettings _settings = new CarAudioSettings();
        [SerializeField, Required] private SoundCue _engineLow;
        [SerializeField, Required] private SoundCue _engineHigh;
        [SerializeField, Required] private SoundCue _wind;
        [SerializeField, Required] private SoundCue _passBy;
        [SerializeField, Required] private SoundCue _signalTick;
        [SerializeField, Required] private SoundCue _signalTock;
        [SerializeField, Required] private SoundCue _scrape;
        [SerializeField, Required] private SoundCue _crashMetal;
        [SerializeField, Required] private SoundCue _crashGlass;

        public CarAudioSettings Settings => _settings;
        public SoundCue EngineLow => _engineLow;
        public SoundCue EngineHigh => _engineHigh;
        public SoundCue Wind => _wind;
        public SoundCue PassBy => _passBy;
        public SoundCue SignalTick => _signalTick;
        public SoundCue SignalTock => _signalTock;
        public SoundCue Scrape => _scrape;
        public SoundCue CrashMetal => _crashMetal;
        public SoundCue CrashGlass => _crashGlass;
    }
}
