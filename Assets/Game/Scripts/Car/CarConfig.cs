using UnityEngine;

namespace WrongLine.Car
{
    /// <summary>The car's numbers. Read every frame, so it can be tuned while the game runs.</summary>
    [CreateAssetMenu(menuName = "WrongLine/Car Config")]
    public sealed class CarConfig : ScriptableObject
    {
        [SerializeField] private CarSettings _settings = new CarSettings();

        public CarSettings Settings => _settings;
    }
}
