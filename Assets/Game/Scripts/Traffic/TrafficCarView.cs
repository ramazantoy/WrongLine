using AF.Core;
using UnityEngine;
using WrongLine.World;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The root of a traffic car prefab: three levels of detail as child objects, only one switched on at a time, and the paint
    /// and turn signals on all of them. Purely visual: the traffic's collisions are the road-space rule's, and
    /// the one solid box it ever gets is for the wreck to land on (<see cref="EnableCrashCollider"/>). Going back to the pool
    /// clears its place and level.
    /// </summary>
    public sealed class TrafficCarView : MonoBehaviour, IPoolable
    {
        public const int LevelCount = 3;

        [Tooltip("The three levels of detail, nearest first.")]
        [SerializeField, Required] private GameObject[] _levels;

        [Tooltip("A soft dark patch on the road under the car: real shadows end at the shadow distance (and are soft or missing under the car), and a car without a contact shadow looks like it floats. Optional.")]
        [SerializeField] private GameObject _blob;

        private MeshRenderer[] _renderers;
        private MeshFilter[] _filters;
        private MeshRenderer[] _bodies;
        private BoxCollider _crashCollider;
        private int _paint;
        private bool _leftSignal;
        private bool _rightSignal;

        /// <summary>The level shown now (0 to 2), or <see cref="TrafficLodRule.None"/> when none is.</summary>
        public int Level { get; private set; } = TrafficLodRule.None;

        public void ShowLevel(int level)
        {
            if (level == Level)
            {
                return;
            }

            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i].SetActive(i == level);
            }

            Level = level;
            SetBlob(level >= 0);
        }

        /// <summary>
        /// Dresses the car in the paint colour at <paramref name="paint"/> of the catalog's palette. Nothing is written into a material:
        /// the choice goes into the renderers' user value (see <see cref="TrafficRendererValue"/>), which the shader reads per draw.
        /// </summary>
        public void SetPaint(int paint)
        {
            if (paint < 0 || paint >= TrafficRendererValue.MaxPaints)
            {
                throw new System.ArgumentOutOfRangeException(nameof(paint), $"Paint {paint} is not in 0..{TrafficRendererValue.MaxPaints - 1}.");
            }

            _paint = paint;
            ApplyValue();
        }

        /// <summary>
        /// Lights the turn signal on the car's <paramref name="side"/> (−1 left, +1 right, 0 none) when <paramref name="lit"/>: the
        /// signal panels of the model glow through its light mask, on every level.
        /// </summary>
        public void ShowSignal(int side, bool lit)
        {
            _leftSignal = lit && side < 0;
            _rightSignal = lit && side > 0;
            ApplyValue();
        }

        private void ApplyValue()
        {
            Cache();
            uint value = TrafficRendererValue.Pack(_paint, _leftSignal, _rightSignal);
            for (int i = 0; i < _bodies.Length; i++)
            {
                if (_bodies[i].GetShaderUserValue() != value)
                {
                    _bodies[i].SetShaderUserValue(value);
                }
            }
        }

        private void SetBlob(bool on)
        {
            if (_blob != null && _blob.activeSelf != on)
            {
                _blob.SetActive(on);
            }
        }

        /// <summary>Grows the culling bounds of every mesh in the car by <paramref name="margin"/> (see <see cref="CurvedBounds"/>).</summary>
        public void ExpandBounds(float margin)
        {
            Cache();
            for (int i = 0; i < _renderers.Length; i++)
            {
                CurvedBounds.Expand(_renderers[i], _filters[i], margin);
            }
        }

        private void Cache()
        {
            if (_renderers != null)
            {
                return;
            }

            _renderers = GetComponentsInChildren<MeshRenderer>(true);
            _filters = new MeshFilter[_renderers.Length];
            var bodies = new System.Collections.Generic.List<MeshRenderer>(_renderers.Length);
            for (int i = 0; i < _renderers.Length; i++)
            {
                _filters[i] = _renderers[i].GetComponent<MeshFilter>();

                // The contact shadow has its own material and takes no paint.
                if (_blob == null || !_renderers[i].transform.IsChildOf(_blob.transform))
                {
                    bodies.Add(_renderers[i]);
                }
            }

            _bodies = bodies.ToArray();
        }

        void IPoolable.OnSpawned(PoolLife life)
        {
        }

        /// <summary>
        /// Switches on a solid box the size of the car, so the wreck of the player's car lands on it instead of passing through. Only
        /// the car the player crashed into gets one; it goes off again when the car returns to the pool.
        /// </summary>
        public void EnableCrashCollider(float length, float halfWidth, float height)
        {
            if (_crashCollider == null)
            {
                _crashCollider = gameObject.AddComponent<BoxCollider>();
            }

            _crashCollider.sharedMaterial = WrongLine.Crash.WreckPhysics.Material;
            _crashCollider.center = new Vector3(0f, height * 0.5f, 0f);
            _crashCollider.size = new Vector3(halfWidth * 2f, height, length);
            _crashCollider.enabled = true;
        }

        void IPoolable.OnDespawned()
        {
            if (_crashCollider != null)
            {
                _crashCollider.enabled = false;
            }

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i].SetActive(false);
            }

            SetBlob(false);
            _leftSignal = false;
            _rightSignal = false;
            ApplyValue();
            Level = TrafficLodRule.None;
        }
    }
}
