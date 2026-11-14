using System;
using System.Collections.Generic;
using AF.Core;
using Cysharp.Threading.Tasks;

namespace WrongLine.Tests
{
    /// <summary>Records the loads instead of loading scenes.</summary>
    internal sealed class FakeSceneLoader : ISceneLoader
    {
        public readonly List<string> Loads = new List<string>();

        public string Current => "Test";
        public bool IsLoading => false;

        public UniTask Load(string scene)
        {
            Loads.Add(scene);
            return UniTask.CompletedTask;
        }

        public UniTask Load<TState>(string scene) where TState : IGameState
        {
            Loads.Add($"{typeof(TState).Name} {scene}");
            return UniTask.CompletedTask;
        }

        public UniTask LoadAdditive(string scene) => throw new NotSupportedException();
        public UniTask UnloadAdditive(string scene) => throw new NotSupportedException();
    }
}
