using System;
using Core.Save.JSON;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Save.SaveStorage
{
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        private const string KeyPrefix = "save_";

        private readonly IJsonService _jsonService;

        public PlayerPrefsSaveStorage(IJsonService jsonService)
        {
            _jsonService = jsonService;
        }

        public UniTask SaveAsync<T>(string key, T data)
        {
            string prefsKey = GetPrefsKey(key);
            string json = _jsonService.Serialize(data);

            PlayerPrefs.SetString(prefsKey, json);
            PlayerPrefs.Save();

            return UniTask.CompletedTask;
        }

        public UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            string prefsKey = GetPrefsKey(key);

            if (!PlayerPrefs.HasKey(prefsKey))
                return UniTask.FromResult(defaultValue);

            try
            {
                string json = PlayerPrefs.GetString(prefsKey);
                T data = _jsonService.Deserialize<T>(json);

                return UniTask.FromResult(data == null ? defaultValue : data);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load PlayerPrefs save '{prefsKey}'. Reason: {exception.Message}");
                return UniTask.FromResult(defaultValue);
            }
        }

        public bool Exists(string key)
        {
            return PlayerPrefs.HasKey(GetPrefsKey(key));
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(GetPrefsKey(key));
            PlayerPrefs.Save();
        }

        private string GetPrefsKey(string key)
        {
            return KeyPrefix + key
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(".", "_");
        }
    }
}