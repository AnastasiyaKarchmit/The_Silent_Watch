using System.Collections.Generic;
using Core.AppStates.Contracts;
using Core.UI.Windows.Contracts;
using UnityEngine;
using VContainer;

namespace Core.AppStates.Components
{
    public abstract class AppStateInstaller : MonoBehaviour, IAppStateInstaller
    {
        [SerializeField] private List<GameObject> objectsToRemoveBeforeInstall = new();

        public abstract void RegisterDependencies(IContainerBuilder builder);

        public virtual void CleanupBeforeInstall()
        {
            foreach (var objectToRemove in objectsToRemoveBeforeInstall)
            {
                if (objectToRemove != null)
                    Destroy(objectToRemove);
            }

            objectsToRemoveBeforeInstall.Clear();
        }
    }
}