using System;
using System.Collections.Generic;
using System.Linq;
using Core.UI.Windows.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.UI.Windows.Config
{
    [CreateAssetMenu(
        fileName = "WindowServiceConfig",
        menuName = "Game Template/Window Service Config")]
    public sealed class WindowServiceConfig : ScriptableObject
    {
        [Header("Root")]
        [SerializeField] private RectTransform windowContainerPrefab;

        [Header("Input")]
        [SerializeField] private GameObject inputBlockerPrefab;

        [Header("Addressable Windows")]
        [SerializeField] private List<WindowConfig> windows = new();

        public RectTransform WindowContainerPrefab => windowContainerPrefab;
        public GameObject InputBlockerPrefab => inputBlockerPrefab;

        public AssetReferenceGameObject GetWindowReference(WindowId id)
        {
            var config = windows.FirstOrDefault(window => window.Id == id);

            if (config == null)
                throw new InvalidOperationException($"Window config for '{id}' was not found.");

            if (config.PrefabReference == null)
                throw new InvalidOperationException($"Window prefab reference for '{id}' is not assigned.");

            return config.PrefabReference;
        }
    }
}