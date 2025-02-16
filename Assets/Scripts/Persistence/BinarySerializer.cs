using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Persistence
{
    public class BinarySerializer : ISerializer
    {
        public TOut Serialize<T, TOut>(T obj)
        {
            if (typeof(TOut) == typeof(byte[]))
            {
                try
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, obj);
                        return (TOut)(object)stream.ToArray(); // Convert to byte[]
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BinarySerializer] Error serializing data: {e.Message}");
                    return default;
                }
            }

            throw new InvalidOperationException($"Unsupported output type: {typeof(TOut)}");
        }

        public T Deserialize<T, TIn>(TIn data)
        {
            if (data is byte[] binaryData)
            {
                try
                {
                    using (MemoryStream stream = new MemoryStream(binaryData))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        return (T)formatter.Deserialize(stream);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BinarySerializer] Error deserializing data: {e.Message}");
                    return default;
                }
            }

            throw new InvalidOperationException($"Unsupported input type: {typeof(TIn)}");
        }
    }
}