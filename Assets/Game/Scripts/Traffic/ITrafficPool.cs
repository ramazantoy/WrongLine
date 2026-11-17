using System.Collections.Generic;
using UnityEngine;

namespace WrongLine.Traffic
{
    /// <summary>The traffic cars waiting to be used: made once, outliving every scene.</summary>
    public interface ITrafficPool
    {
        /// <summary>What the layout needs to know of each model, in catalog order (a model's index is its id).</summary>
        IReadOnlyList<TrafficModelSpec> Models { get; }

        int PaintCount { get; }

        int CountActive { get; }

        Material Paint(int index);

        /// <summary>Makes the cars of each model the run can need at most. Doing it again does nothing.</summary>
        void Prewarm();

        TrafficCarView Spawn(int model, Vector3 position, Quaternion rotation);

        void Release(TrafficCarView view);

        void ReleaseAll();
    }
}
