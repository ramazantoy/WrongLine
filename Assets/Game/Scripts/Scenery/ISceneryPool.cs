using UnityEngine;

namespace WrongLine.Scenery
{
    /// <summary>The roadside props waiting to be used: made once, outliving every scene.</summary>
    public interface ISceneryPool
    {
        ScenerySet Set { get; }

        int CountActive { get; }

        /// <summary>Makes the configured number of each prop. Doing it again does nothing.</summary>
        void Prewarm();

        SceneryPropView Spawn(int propId, Vector3 position, Quaternion rotation, float scale);

        void Release(SceneryPropView view);

        void ReleaseAll();
    }
}
