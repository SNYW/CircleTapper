namespace Persistence
{
    public interface ISerializer
    {
        TOut Serialize<T, TOut>(T obj);
        T Deserialize<T, TIn>(TIn data);
    }
}