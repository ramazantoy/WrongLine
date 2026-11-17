using System;
using UnityEngine;

namespace WrongLine.Traffic
{
    /// <summary>
    /// The traffic car models (one prefab each, with its length and how often it comes up) and the paint materials a car may wear.
    /// The prefabs are rebuilt by <c>WrongLine/Traffic/Rebuild Traffic</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "WrongLine/Traffic Catalog")]
    public sealed class TrafficCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string _name;
            [SerializeField] private TrafficCarView _prefab;

            [Tooltip("Bumper to bumper, in metres.")]
            [SerializeField, Min(0.5f)] private float _length = 4.5f;

            [Tooltip("How often this model comes up, against the others.")]
            [SerializeField, Min(0f)] private float _weight = 1f;

            [Tooltip("Where the wheels touch the road: the front and rear axles along the car's forward axis from its pivot, and half the distance between the wheels of an axle, in metres. Written by Rebuild Traffic from the model.")]
            [SerializeField] private float _frontAxle = 1.3f;

            [SerializeField] private float _rearAxle = -1.3f;

            [SerializeField] private float _halfTrack = 0.75f;

            public string Name => _name;

            public TrafficCarView Prefab => _prefab;

            public float Length => _length;

            public float Weight => _weight;

            public float FrontAxle => _frontAxle;

            public float RearAxle => _rearAxle;

            public float HalfTrack => _halfTrack;
        }

        [SerializeField] private Entry[] _models = new Entry[0];

        [Tooltip("One material per paint colour: the atlas with the paint mask and a different Paint Color. Built by Rebuild Traffic.")]
        [SerializeField] private Material[] _paints = new Material[0];

        public Entry[] Models => _models;

        public Material[] Paints => _paints;

        /// <exception cref="ArgumentException">The catalog cannot be used.</exception>
        public void Validate()
        {
            if (_models.Length == 0)
            {
                throw new ArgumentException("The traffic catalog has no model.");
            }

            if (_paints.Length == 0)
            {
                throw new ArgumentException("The traffic catalog has no paint.");
            }

            float total = 0f;
            foreach (Entry model in _models)
            {
                if (model.Prefab == null || !(model.Length > 0f) || !(model.Weight >= 0f))
                {
                    throw new ArgumentException($"Traffic model '{model.Name}' needs a prefab, a positive length and a weight that is not negative.");
                }

                total += model.Weight;
            }

            if (!(total > 0f))
            {
                throw new ArgumentException("The traffic models' weights add up to nothing.");
            }

            foreach (Material paint in _paints)
            {
                if (paint == null)
                {
                    throw new ArgumentException("A traffic paint is missing its material.");
                }
            }
        }
    }
}
