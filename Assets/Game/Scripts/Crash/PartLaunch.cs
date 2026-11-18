using UnityEngine;

namespace WrongLine.Crash
{
    /// <summary>How one part of the wrecked car flies off, in the car's own axes (x right, y up, z forward).</summary>
    public readonly struct PartLaunch
    {
        /// <summary>Metres per second.</summary>
        public readonly Vector3 Linear;

        /// <summary>Degrees per second about each axis.</summary>
        public readonly Vector3 Angular;

        public PartLaunch(Vector3 linear, Vector3 angular)
        {
            Linear = linear;
            Angular = angular;
        }
    }
}
