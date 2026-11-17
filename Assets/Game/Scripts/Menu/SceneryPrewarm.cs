using VContainer.Unity;
using WrongLine.Scenery;

namespace WrongLine.Menu
{
    /// <summary>Makes the roadside props while the menu is up, so the run does not stall creating them.</summary>
    internal sealed class SceneryPrewarm : IStartable
    {
        private readonly ISceneryPool _pool;

        public SceneryPrewarm(ISceneryPool pool)
        {
            _pool = pool;
        }

        public void Start()
        {
            _pool.Prewarm();
        }
    }
}
