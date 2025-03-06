using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ObjectPooling
{
    [CreateAssetMenu(fileName = "new Pool", menuName = "Game Data/ Object Pool")]
    public class ObjectPool : ScriptableObject
    {
        public ObjectPoolName poolName;
        [SerializeField] private GameObject pooledObject;
        [SerializeField] private int minAmount;

        private Transform _pooledObjectParent;

        private List<GameObject> _pool;

        public GameObject GetPooledObject()
        {
            foreach (var go in _pool)
            {
                if (!go.activeInHierarchy)
                {
                    return go;
                }
            }

            var newPooledObject = Instantiate(pooledObject, Vector2.zero, Quaternion.identity, _pooledObjectParent);
            newPooledObject.SetActive(false);
            _pool.Add(newPooledObject);
            return newPooledObject;
        }

        public void InitPool()
        {
            _pooledObjectParent = GameObject.Find("Pooled Objects").transform;
            
            if (_pool != null && _pool.Any())
            {
                _pool.ForEach(Destroy);
                _pool.Clear();
            }
            else
            {
                _pool = new List<GameObject>();
            }
            
            for (int i = 0; i < minAmount; i++)
            {
                var newPooledObject = Instantiate(
                    pooledObject, 
                    Vector2.zero,
                    Quaternion.identity, 
                    _pooledObjectParent
                );
                
                newPooledObject.SetActive(false);
                _pool.Add(newPooledObject);
            }
        }

        public int GetActiveAmount()
        {
            int count = 0;
            foreach (var o in _pool)
            {
                if (o.activeInHierarchy) count++;
            }

            return count;
        }

        public List<GameObject> GetAllActive()
        {
            return _pool.Where(o => o.activeInHierarchy).ToList();
        }

        public enum ObjectPoolName
        {
            SoundPlayer
        }
    }
}
