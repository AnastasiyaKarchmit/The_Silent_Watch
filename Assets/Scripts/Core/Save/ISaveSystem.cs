using Cysharp.Threading.Tasks;

namespace Core.Save
{
    public interface ISaveSystem
    {
        bool IsLoaded { get; }
        PersistentData Data { get; }

        void Register(ISaveDataProvider provider);
        void Unregister(ISaveDataProvider provider);

        UniTask LoadAsync();
        UniTask SaveAsync();
        UniTask ResetAsync();
    }
}