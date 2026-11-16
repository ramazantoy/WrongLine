using AF.Core;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer.Unity;

namespace WrongLine.World
{
    /// <summary>
    /// Plays the day and night cycle: each frame it evaluates the rule and writes the sky colours as shader globals (the sky
    /// material is never written to), the fog and ambient colours into the scene's render settings, and the sun light.
    /// A run starts at the config's start phase; the clock only counts game time, so a pause holds the sky still.
    /// </summary>
    internal sealed class DayNightDriver : IStartable, IPostLateTickable
    {
        private static readonly int _zenithId = Shader.PropertyToID("_WL_SkyZenith");
        private static readonly int _horizonId = Shader.PropertyToID("_WL_SkyHorizon");
        private static readonly int _groundId = Shader.PropertyToID("_WL_SkyGround");
        private static readonly int _sunDirectionId = Shader.PropertyToID("_WL_SunDirection");
        private static readonly int _sunColorId = Shader.PropertyToID("_WL_SunColor");

        private readonly DayNightConfig _config;
        private readonly Light _sun;
        private readonly IGameClock _clock;
        private DayNightRule _rule;
        private float _elapsed;

        public DayNightDriver(DayNightConfig config, Light sun, IGameClock clock)
        {
            _config = config;
            _sun = sun;
            _clock = clock;
        }

        public void Start()
        {
            // The config is built into the rule once; its settings object is read live, so Inspector tuning still shows.
            _rule = new DayNightRule(_config.Settings);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Apply();
        }

        public void PostLateTick()
        {
            _elapsed += _clock.DeltaTime;
            Apply();
        }

        private void Apply()
        {
            SkyState sky = _rule.Evaluate(_rule.PhaseAt(_elapsed));

            Shader.SetGlobalColor(_zenithId, sky.Zenith);
            Shader.SetGlobalColor(_horizonId, sky.Horizon);
            Shader.SetGlobalColor(_groundId, sky.Ground);

            RenderSettings.fogColor = sky.Fog;
            RenderSettings.ambientSkyColor = sky.AmbientSky;
            RenderSettings.ambientEquatorColor = sky.AmbientEquator;
            RenderSettings.ambientGroundColor = sky.AmbientGround;

            _sun.color = sky.LightColor;
            _sun.intensity = sky.LightIntensity;
            _sun.transform.rotation = sky.LightRotation;

            // The sky body sits where the light comes from.
            Vector3 towardsSun = -_sun.transform.forward;
            Shader.SetGlobalVector(_sunDirectionId, new Vector4(towardsSun.x, towardsSun.y, towardsSun.z, 0f));
            Shader.SetGlobalColor(_sunColorId, sky.LightColor);
        }
    }
}
