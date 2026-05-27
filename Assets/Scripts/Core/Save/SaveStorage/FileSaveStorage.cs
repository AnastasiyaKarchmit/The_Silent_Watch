using System;
using System.IO;
using Core.Save.JSON;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Save.SaveStorage
{
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly IJsonService _jsonService;

        public FileSaveStorage(IJsonService jsonService)
        {
            _jsonService = jsonService;
        }

        public async UniTask SaveAsync<T>(string key, T data)
        {
            string path = GetPath(key);
            string tempPath = path + ".tmp";

            string json = _jsonService.Serialize(data);

            await UniTask.RunOnThreadPool(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                    File.Delete(path);

                File.Move(tempPath, path);
            });
        }

        public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            string path = GetPath(key);

            if (!File.Exists(path))
                return defaultValue;

            try
            {
                string json = await UniTask.RunOnThreadPool(() => File.ReadAllText(path));
                T data = _jsonService.Deserialize<T>(json);

                return data == null ? defaultValue : data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load file save '{key}'. Reason: {exception.Message}");
                return defaultValue;
            }
        }

        public bool Exists(string key)
        {
            return File.Exists(GetPath(key));
        }

        public void Delete(string key)
        {
            string path = GetPath(key);

            if (File.Exists(path))
                File.Delete(path);
        }

        private string GetPath(string key)
        {
            return Path.Combine(UnityEngine.Application.persistentDataPath, key);
        }
    }
}