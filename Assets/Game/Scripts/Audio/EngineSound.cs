namespace WrongLine.Audio
{
    /// <summary>What the engine's and the road's loops should sound like this frame.</summary>
    public readonly struct EngineSound
    {
        public readonly float Revs;
        public readonly float Pitch;
        public readonly float LowVolume;
        public readonly float HighVolume;
        public readonly float WindVolume;
        public readonly float WindPitch;

        public EngineSound(float revs, float pitch, float lowVolume, float highVolume, float windVolume, float windPitch)
        {
            Revs = revs;
            Pitch = pitch;
            LowVolume = lowVolume;
            HighVolume = highVolume;
            WindVolume = windVolume;
            WindPitch = windPitch;
        }
    }
}
