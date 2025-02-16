using UnityEngine;

namespace Persistence
{
    public class JsonSerializer : ISerializer
    {
        public TOut Serialize<T, TOut>(T obj)
        {
            if (typeof(TOut) == typeof(string))
            {
                return (TOut)(object)JsonUtility.ToJson(obj); // Convert to string
            }

            throw new System.InvalidOperationException($"Unsupported output type: {typeof(TOut)}");
        }

        public T Deserialize<T, TIn>(TIn data)
        {
            if (data is string json)
            {
                return JsonUtility.FromJson<T>(json);
            }

            throw new System.InvalidOperationException($"Unsupported input type: {typeof(TIn)}");
        }
    }
}