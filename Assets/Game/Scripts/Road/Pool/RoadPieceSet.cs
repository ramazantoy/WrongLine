using System;
using System.Collections.Generic;

namespace WrongLine.Road
{
    /// <summary>
    /// The catalog as the generator and the pool use it: a piece id (0 is the start piece, the selectable pieces follow in
    /// catalog order) and the prefab it stands for.
    /// </summary>
    public sealed class RoadPieceSet
    {
        private RoadPieceSet(List<RoadPieceInfo> infos, List<RoadPieceView> prefabs)
        {
            Infos = infos;
            Prefabs = prefabs;
        }

        public IReadOnlyList<RoadPieceInfo> Infos { get; }

        /// <summary>The prefab of each piece, indexed by piece id.</summary>
        public IReadOnlyList<RoadPieceView> Prefabs { get; }

        public RoadPieceInfo Start => Infos[0];

        public static RoadPieceSet From(RoadCatalog catalog)
        {
            if (catalog.Start == null)
            {
                throw new ArgumentException("The road catalog has no start piece.", nameof(catalog));
            }

            var infos = new List<RoadPieceInfo>();
            var prefabs = new List<RoadPieceView>();
            Add(infos, prefabs, catalog.Start, isStart: true);
            for (int i = 0; i < catalog.Pieces.Count; i++)
            {
                if (catalog.Pieces[i] == null)
                {
                    throw new ArgumentException($"The road catalog has an empty piece slot ({i}).", nameof(catalog));
                }

                Add(infos, prefabs, catalog.Pieces[i], isStart: false);
            }

            return new RoadPieceSet(infos, prefabs);
        }

        private static void Add(List<RoadPieceInfo> infos, List<RoadPieceView> prefabs, RoadPieceView prefab, bool isStart)
        {
            if (prefab.Recipe == null)
            {
                throw new ArgumentException($"The road piece '{prefab.name}' has no recipe.");
            }

            infos.Add(new RoadPieceInfo(infos.Count, prefab.name, prefab.Recipe.Recipe, isStart));
            prefabs.Add(prefab);
        }
    }
}
