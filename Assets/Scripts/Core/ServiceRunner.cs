using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Drives <see cref="ITickable"/> services. Services are plain C# objects with no Unity
    /// lifecycle, so this is the single MonoBehaviour that gives them a heartbeat. Added by
    /// <see cref="GameBootstrapper"/> once initialization completes.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class ServiceRunner : MonoBehaviour
    {
        private readonly List<ITickable> _tickables = new();

        /// <summary>Caches the tickable subset of the registry. Call once, after initialization.</summary>
        public void Bind(IReadOnlyList<IGameService> services)
        {
            _tickables.Clear();

            foreach (IGameService service in services)
            {
                if (service is ITickable tickable) _tickables.Add(tickable);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _tickables.Count; i++)
            {
                _tickables[i].Tick(deltaTime);
            }
        }
    }
}
