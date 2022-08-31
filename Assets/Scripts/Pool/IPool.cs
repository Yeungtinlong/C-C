using CNC.Factory;

namespace CNC.Pool
{
    public interface IPool<T>
    {
        IFactory<T> Factory { get; set; }
        T Pop();
        void Push(T item);
    }
}

