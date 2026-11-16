using System.Collections.Generic;
using System.Linq;
using WrongLine.Road.Editor;

namespace WrongLine.Road.Tests
{
    /// <summary>The default piece set as generator input, built without any asset.</summary>
    internal static class RoadTestPieces
    {
        public static List<RoadPieceInfo> All()
        {
            return RoadPieceSpecs.Defaults.Select((spec, index) => new RoadPieceInfo(index, spec.Name, spec.Recipe, spec.IsStart)).ToList();
        }

        public static RoadPieceInfo Start(List<RoadPieceInfo> pieces)
        {
            return pieces.First(piece => piece.IsStart);
        }

        public static RoadLayout Layout(ulong seed, RoadGeneratorSettings settings = null, List<RoadPieceInfo> pieces = null)
        {
            pieces = pieces ?? All();
            var generator = new RoadGenerator(pieces, settings ?? new RoadGeneratorSettings(), seed);
            return new RoadLayout(Start(pieces), generator);
        }

        public static RoadPieceInfo Named(List<RoadPieceInfo> pieces, string name)
        {
            return pieces.First(piece => piece.Name == name);
        }
    }
}
