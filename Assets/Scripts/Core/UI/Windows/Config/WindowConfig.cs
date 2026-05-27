using System;
using Core.UI.Windows.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.UI.Windows.Config
{
    [Serializable]
    public sealed class WindowConfig
    {
        [SerializeField] private WindowId id;
        [SerializeField] private AssetReferenceGameObject prefabReference;

        public WindowId Id => id;
        public AssetReferenceGameObject PrefabReference => prefabReference;
    }
}