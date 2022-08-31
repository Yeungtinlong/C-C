namespace CNC.Factory
{
    public interface IFactory<T>
    {
        T Create();
    }
}