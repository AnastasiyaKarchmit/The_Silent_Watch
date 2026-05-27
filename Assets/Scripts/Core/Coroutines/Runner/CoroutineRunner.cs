using Infrastructure.Coroutines.Runner.Core;
using UnityEngine;

namespace Infrastructure.Coroutines.Runner
{
    public class CoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        private void Awake() => DontDestroyOnLoad(gameObject);
    }
}