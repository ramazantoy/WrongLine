using System;
using AF.Core;
using WrongLine.Road;

namespace WrongLine.Driving
{
    /// <summary>The seed of this run: the road and the sky both grow from it, so a seed in a bug report brings back both.</summary>
    public interface IRunSeed
    {
        ulong Value { get; }
    }

    /// <summary>Resolves the seed once, when the run's scope builds: the configured one, or a new one from the clock when the config says 0.</summary>
    internal sealed class RunSeed : IRunSeed
    {
        public RunSeed(RoadConfig road)
        {
            Value = RoadSeed.Resolve(road.Seed, unchecked((ulong)DateTime.UtcNow.Ticks));
            Log.Info($"[Road] Run seed {Value}.");
        }

        public ulong Value { get; }
    }
}
