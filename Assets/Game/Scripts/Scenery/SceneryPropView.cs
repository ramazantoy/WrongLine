using AF.Core;
using UnityEngine;
using WrongLine.World;

namespace WrongLine.Scenery
{
    /// <summary>
    /// The root of a roadside prop prefab; its pivot is the middle of its base. Purely visual: no collider, nothing reads it.
    /// Going back to the pool clears its place and size, so a waiting prop holds nothing from its last use.
    /// </summary>
    public sealed class SceneryPropView : MonoBehaviour, IPoolable
    {
        private MeshRenderer[] _renderers;
        private MeshFilter[] _filters;

        /// <summary>Grows the culling bounds of every mesh in the prop by <paramref name="margin"/> (see <see cref="CurvedBounds"/>).</summary>
        public void ExpandBounds(float margin)
        {
            if (_renderers == null)
            {
                _renderers = GetComponentsInChildren<MeshRenderer>(true);
                _filters = new MeshFilter[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _filters[i] = _renderers[i].GetComponent<MeshFilter>();
                }
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                CurvedBounds.Expand(_renderers[i], _filters[i], margin);
            }
        }

        void IPoolable.OnSpawned(PoolLife life)
        {
        }

        void IPoolable.OnDespawned()
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }
    }
}
