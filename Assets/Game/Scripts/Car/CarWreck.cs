using AF.Core;
using UnityEngine;
using WrongLine.Crash;

namespace WrongLine.Car
{
    /// <summary>
    /// Turns the car's model into a wreck: the body and the loose parts leave the car's hierarchy and become rigid bodies that
    /// fly off as <see cref="WreckRule"/> says; the doors, the hood and the trunk stay on their hinges until the hinge breaks.
    /// Parts are found by the model's node names (the artist's contract, as the wheels and the steering wheel are).
    /// </summary>
    internal static class CarWreck
    {
        private const string BODY = "Body";

        /// <summary>Parts that stay on the body: they ride with it.</summary>
        private static readonly string[] Riders = { "Interior", "SteeringWheel" };

        /// <summary>Parts that come off on a hinge: the part, its hinge node, and the hinge's axis in the part's own space.</summary>
        private static readonly (string Part, string Hinge, Vector3 Axis)[] Hinged =
        {
            ("Door_FL", "Hinge_FL", Vector3.up),
            ("Door_FR", "Hinge_FR", Vector3.up),
            ("Door_RL", "Hinge_RL", Vector3.up),
            ("Door_RR", "Hinge_RR", Vector3.up),
            ("Hood", "Hinge_Hood", Vector3.right),
            ("Trunk", "Hinge_Trunk", Vector3.right),
        };

        /// <summary>Parts that come off free.</summary>
        private static readonly string[] Loose =
        {
            "Bumper_F", "Bumper_R", "Mirror_L", "Mirror_R", "Spoiler", "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR",
        };

        /// <summary>Greatest swing of a door, hood or trunk on its hinge, in degrees.</summary>
        private const float HingeSwing = 80f;

        /// <returns>The body (the wreck's middle), or null when the model has none.</returns>
        public static Transform Break(Transform root, WreckRule wreck, CrashSettings.WreckGroup settings)
        {
            Transform body = Find(root, BODY);
            if (body == null)
            {
                Log.Error("CarWreck: the car model has no 'Body' node.");
                return null;
            }

            Quaternion facing = body.rotation;
            var riders = new Transform[Riders.Length];
            for (int i = 0; i < Riders.Length; i++)
            {
                riders[i] = Find(root, Riders[i]);
            }

            // Everything that becomes a rigid body leaves the hierarchy (a rigid body under a car that is not one would not move freely).
            Rigidbody bodyRigid = Solidify(body, settings.BodyMass, settings.Damping);
            Collider bodyBox = body.GetComponent<Collider>();
            PartLaunch bodyLaunch = wreck.Launch(0);
            Launch(bodyRigid, facing, bodyLaunch.Linear * settings.BodyShare, bodyLaunch.Angular * settings.BodyShare);
            foreach (Transform rider in riders)
            {
                if (rider != null)
                {
                    rider.SetParent(body, true);
                }
            }

            int index = 1;
            foreach ((string part, string hinge, Vector3 axis) in Hinged)
            {
                Transform node = Find(root, part);
                if (node == null)
                {
                    continue;
                }

                Transform pivot = Find(node, hinge);
                Vector3 anchor = pivot != null ? pivot.localPosition : Vector3.zero;
                Rigidbody rigid = Solidify(node, settings.PartMass, settings.Damping);
                IgnoreBody(node, bodyBox);
                index++;

                // A hinged part starts with the body's own motion: a different speed or spin would tear the hinge off in the first
                // frame (the force is the speed difference over the frame time). Hits against the ground and the traffic break it later.
                Launch(rigid, facing, bodyLaunch.Linear * settings.BodyShare, Vector3.zero);

                var joint = node.gameObject.AddComponent<HingeJoint>();
                joint.connectedBody = bodyRigid;
                joint.anchor = anchor;
                joint.axis = axis;
                joint.breakForce = settings.BreakForce;
                joint.breakTorque = settings.BreakForce;
                joint.useLimits = true;
                joint.limits = new JointLimits { min = -HingeSwing, max = HingeSwing };
            }

            foreach (string part in Loose)
            {
                Transform node = Find(root, part);
                if (node == null)
                {
                    continue;
                }

                Rigidbody rigid = Solidify(node, settings.PartMass, settings.Damping);
                IgnoreBody(node, bodyBox);
                PartLaunch launch = wreck.Launch(index++);
                Launch(rigid, facing, launch.Linear, launch.Angular);
            }

            return body;
        }

        /// <summary>The parts start inside the body's box (it is the car's whole outline): they must not push against it.</summary>
        private static void IgnoreBody(Transform part, Collider bodyBox)
        {
            Collider box = part.GetComponent<Collider>();
            if (box != null && bodyBox != null)
            {
                Physics.IgnoreCollision(box, bodyBox);
            }
        }

        private static Rigidbody Solidify(Transform node, float mass, float damping)
        {
            node.SetParent(null, true);

            var filter = node.GetComponent<MeshFilter>();
            var box = node.gameObject.AddComponent<BoxCollider>();
            box.sharedMaterial = WreckPhysics.Material;
            if (filter != null && filter.sharedMesh != null)
            {
                box.center = filter.sharedMesh.bounds.center;
                box.size = filter.sharedMesh.bounds.size;
            }

            var rigid = node.gameObject.AddComponent<Rigidbody>();
            rigid.mass = mass;
            rigid.linearDamping = damping;
            rigid.angularDamping = damping * 2f;
            rigid.interpolation = RigidbodyInterpolation.Interpolate;
            rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return rigid;
        }

        private static void Launch(Rigidbody rigid, Quaternion facing, Vector3 linear, Vector3 angularDegrees)
        {
            rigid.linearVelocity = facing * linear;
            rigid.angularVelocity = facing * angularDegrees * Mathf.Deg2Rad;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
