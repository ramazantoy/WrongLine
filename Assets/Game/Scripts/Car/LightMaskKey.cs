namespace WrongLine.Car
{
    /// <summary>What a painted patch of the car's texture is, by its colour (see <see cref="LightMaskKey"/>).</summary>
    public enum LightPanel
    {
        None,
        Headlight,
        Stop,
        Signal,
    }

    /// <summary>
    /// The key the car's light mask is cut with. The model's lights are painted as flat, pure colours (a warm white headlight, a
    /// saturated red stop light, an amber turn signal) that the body paint, the glass and the dark parts do not reach, so a texel's
    /// colour says whether it is a light. Which side a signal is on comes from where the surface is on the car, not from the colour.
    /// </summary>
    public static class LightMaskKey
    {
        private const float HeadlightRedMin = 0.9f;
        private const float HeadlightGreenMin = 0.8f;
        // A generated model's headlight can come out a pale yellow (down to about 0.5 blue).
        private const float HeadlightBlueMin = 0.5f;

        // Generated models paint the brake light a little darker than the flat red they were drawn with (down to about 0.7).
        private const float StopRedMin = 0.65f;
        private const float StopGreenMax = 0.25f;
        private const float StopBlueMax = 0.25f;

        private const float SignalRedMin = 0.8f;
        private const float SignalGreenMin = 0.5f;
        private const float SignalGreenMax = 0.75f;
        private const float SignalBlueMax = 0.3f;

        public static LightPanel Classify(float red, float green, float blue)
        {
            if (red >= HeadlightRedMin && green >= HeadlightGreenMin && blue >= HeadlightBlueMin)
            {
                return LightPanel.Headlight;
            }

            if (red >= StopRedMin && green <= StopGreenMax && blue <= StopBlueMax)
            {
                return LightPanel.Stop;
            }

            if (red >= SignalRedMin && green >= SignalGreenMin && green <= SignalGreenMax && blue <= SignalBlueMax)
            {
                return LightPanel.Signal;
            }

            return LightPanel.None;
        }
    }
}
