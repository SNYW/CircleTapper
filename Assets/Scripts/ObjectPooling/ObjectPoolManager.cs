using System.Collections.Generic;
using System.Linq;
using ObjectPooling;
using UnityEngine;

public static class ObjectPoolManager
{
    private static Dictionary<ObjectPool.ObjectPoolName, ObjectPool> _pools;

    public static ObjectPool GetPool(ObjectPool.ObjectPoolName poolName)
    {
        return _pools.TryGetValue(poolName, out var pool) ? pool : null;
    }

    public static void InitPools()
    {
        var allPools = Resources.LoadAll("Data/Pools", typeof(ObjectPool)).Cast<ObjectPool>();

        _pools = new Dictionary<ObjectPool.ObjectPoolName, ObjectPool>();
        
        foreach (var objectPool in allPools)
        {
          objectPool.InitPool();
          _pools.Add(objectPool.poolName, objectPool);
        }
    }
}
