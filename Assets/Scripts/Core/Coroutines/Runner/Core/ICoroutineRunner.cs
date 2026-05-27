using System.Collections;
using UnityEngine;

namespace Infrastructure.Coroutines.Runner.Core
{
    public interface ICoroutineRunner
    {
        public Coroutine StartCoroutine(IEnumerator routine);

        public void StopCoroutine(Coroutine routine);
    }
}