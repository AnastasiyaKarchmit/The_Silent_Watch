using Cysharp.Threading.Tasks;

namespace Core.Save
{
    public interface ISaveDataProvider
    {
        UniTask LoadAsync(PersistentData data);
        void Save(PersistentData data);
    }
}