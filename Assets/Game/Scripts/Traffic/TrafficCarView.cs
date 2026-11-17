using AF.Core;
using UnityEngine;
using WrongLine.World;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The root of a traffic car prefab: three levels of detail as child objects (LOD0 has the four wheels under it), only one
    /// switched on at a time, and the paint on all of them. Purely visual: no collider, nothing reads it. Going back to the pool
    /// clears its place and level.
    /// </summary>
    public sealed class TrafficCarView : MonoBehaviour, IPoolable
    {
        public const int LevelCount = 3;

        [Tooltip("The three levels of detail, nearest first.")]
        [SerializeField, Required] private GameObject[] _levels;

        private MeshRenderer[] _renderers;
        private MeshFilter[] _filters;

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
        }

        /// <summary>Paints every mesh of the car with <paramref name="paint"/> (a shared material: nothing is written into it).</summary>
        public void SetPaint(Material paint)
        {
            Cache();
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterial = paint;
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

        void IPoolable.OnDespawned()
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i].SetActive(false);
            }

            Level = TrafficLodRule.None;
        }
    }
}
