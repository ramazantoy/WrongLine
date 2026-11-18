using AF.Core;
using UnityEngine;
using WrongLine.World;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The root of a traffic car prefab: three levels of detail as child objects (LOD0 has the four wheels under it), only one
    /// switched on at a time, and the paint on all of them. Purely visual: the traffic's collisions are the road-space rule's, and
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

        [Tooltip("The parent of the signal lamps (kept out of the paint), and the lamps themselves, on the car's left and right. Optional.")]
        [SerializeField] private Transform _signalRoot;

        [SerializeField] private GameObject[] _leftLamps = new GameObject[0];

        [SerializeField] private GameObject[] _rightLamps = new GameObject[0];

        private MeshRenderer[] _renderers;
        private MeshFilter[] _filters;
        private BoxCollider _crashCollider;

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

        /// <summary>Paints every mesh of the car with <paramref name="paint"/> (a shared material: nothing is written into it).</summary>
        public void SetPaint(Material paint)
        {
            Cache();
            for (int i = 0; i < _renderers.Length; i++)
            {
                // The blob and the lamps keep their own materials.
                Transform part = _renderers[i].transform;
                if ((_blob != null && part.IsChildOf(_blob.transform)) || (_signalRoot != null && part.IsChildOf(_signalRoot)))
                {
                    continue;
                }

                _renderers[i].sharedMaterial = paint;
            }
        }

        /// <summary>
        /// Lights the turn signal on the car's <paramref name="side"/> (−1 left, +1 right, 0 none) when <paramref name="lit"/>. The lamps
        /// show on the two nearest levels only: the far level is too small for them.
        /// </summary>
        public void ShowSignal(int side, bool lit)
        {
            bool visible = lit && Level >= 0 && Level <= 1;
            SetLamps(_leftLamps, visible && side < 0);
            SetLamps(_rightLamps, visible && side > 0);
        }

        private static void SetLamps(GameObject[] lamps, bool on)
        {
            foreach (GameObject lamp in lamps)
            {
                if (lamp.activeSelf != on)
                {
                    lamp.SetActive(on);
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
            for (int i = 0; i < _renderers.Length; i++)
            {
                _filters[i] = _renderers[i].GetComponent<MeshFilter>();
            }
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
            SetLamps(_leftLamps, false);
            SetLamps(_rightLamps, false);
            Level = TrafficLodRule.None;
        }
    }
}
