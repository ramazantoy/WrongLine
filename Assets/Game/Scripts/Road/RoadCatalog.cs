using System.Collections.Generic;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>The set of road pieces a run is built from: the fixed start piece and the pieces the generator chooses among.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Road Catalog")]
    public sealed class RoadCatalog : ScriptableObject
    {
        [SerializeField] private RoadPieceView _start;
        [SerializeField] private List<RoadPieceView> _pieces = new List<RoadPieceView>();

        public RoadPieceView Start => _start;

        public IReadOnlyList<RoadPieceView> Pieces => _pieces;
    }
}
