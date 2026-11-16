using VContainer.Unity;
using WrongLine.Road;

namespace WrongLine.Menu
{
    /// <summary>Makes the road pieces while the menu is up, so the run does not stall creating them.</summary>
    internal sealed class RoadPrewarm : IStartable
    {
        private readonly IRoadPiecePool _pool;

        public RoadPrewarm(IRoadPiecePool pool)
        {
            _pool = pool;
        }

        public void Start()
        {
            _pool.Prewarm();
        }
    }
}
