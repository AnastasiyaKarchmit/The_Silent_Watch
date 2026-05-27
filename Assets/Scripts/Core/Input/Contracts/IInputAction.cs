using R3;

namespace Core.Input.Contracts
{
    public interface IInputAction<T>
    {
        bool Enabled { get; }

        T Value { get; }

        Observable<T> Started { get; }
        Observable<T> Performed { get; }
        Observable<T> Canceled { get; }

        void SetActive(bool active);
    }
}