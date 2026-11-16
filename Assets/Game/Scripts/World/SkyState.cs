using UnityEngine;

namespace WrongLine.World
{
    /// <summary>What the sky, the fog, the ambient light and the sun look like at one moment of the day.</summary>
    public readonly struct SkyState
    {
        public SkyState(Color zenith, Color horizon, Color ground, Color lightColor, float lightIntensity, float lightPitch, float lightYaw, Color ambientSky, Color ambientEquator, Color ambientGround)
        {
            Zenith = zenith;
            Horizon = horizon;
            Ground = ground;
            LightColor = lightColor;
            LightIntensity = lightIntensity;
            LightPitch = lightPitch;
            LightYaw = lightYaw;
            AmbientSky = ambientSky;
            AmbientEquator = ambientEquator;
            AmbientGround = ambientGround;
        }

        public Color Zenith { get; }

        public Color Horizon { get; }

        public Color Ground { get; }

        public Color LightColor { get; }

        public float LightIntensity { get; }

        /// <summary>Degrees the light is tilted down from the horizontal.</summary>
        public float LightPitch { get; }

        public float LightYaw { get; }

        public Color AmbientSky { get; }

        public Color AmbientEquator { get; }

        public Color AmbientGround { get; }

        /// <summary>The fog is always the horizon colour: the far road dissolves into the sky with no seam.</summary>
        public Color Fog => Horizon;

        public Quaternion LightRotation => Quaternion.Euler(LightPitch, LightYaw, 0f);
    }
}
