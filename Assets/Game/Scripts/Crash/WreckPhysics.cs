using UnityEngine;

namespace WrongLine.Crash
{
    /// <summary>
    /// The physics material of the wreck: the body, the parts and the car the player hit are a little bouncy, so the wreck rebounds and
    /// tumbles off what it hit instead of stopping dead against it (which looked fake). Made once, when first needed.
    /// </summary>
    internal static class WreckPhysics
    {
        private const float Bounciness = 0.35f;
        private const float Friction = 0.5f;

        private static PhysicsMaterial _material;

        public static PhysicsMaterial Material
        {
            get
            {
                if (_material == null)
                {
                    _material = new PhysicsMaterial("Wreck")
                    {
                        bounciness = Bounciness,
                        dynamicFriction = Friction,
                        staticFriction = Friction,
                        bounceCombine = PhysicsMaterialCombine.Maximum,
                        frictionCombine = PhysicsMaterialCombine.Average,
                    };
                }

                return _material;
            }
        }
    }
}
