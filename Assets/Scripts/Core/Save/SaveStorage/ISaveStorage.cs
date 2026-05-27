using Cysharp.Threading.Tasks;

namespace Core.Save.SaveStorage
{
    public interface ISaveStorage
    {
        UniTask<T> LoadAsync<T>(string key, T defaultValue = default);
        UniTask SaveAsync<T>(string key, T data);
        bool Exists(string key);
        void Delete(string key);
    }
}