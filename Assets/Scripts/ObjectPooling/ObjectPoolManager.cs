using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ObjectPooling
{
    public static class ObjectPoolManager
    {
        private static Dictionary<ObjectPool.ObjectPoolName, ObjectPool> _pools;

        public static ObjectPool GetPool(ObjectPool.ObjectPoolName poolName)
        {
            return _pools.GetValueOrDefault(poolName);
        }

        public static void InitPools()
        {
            var allPools = Resources.LoadAll("Data/Pools", typeof(ObjectPool)).Cast<ObjectPool>();
            var pooledObjectParent = new GameObject("Pooled Objects");
            Object.DontDestroyOnLoad(pooledObjectParent.gameObject);

            _pools = new Dictionary<ObjectPool.ObjectPoolName, ObjectPool>();
        
            foreach (var objectPool in allPools)
            {
                objectPool.InitPool(pooledObjectParent);
                _pools.Add(objectPool.poolName, objectPool);
            }
        }
    }
}
