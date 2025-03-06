using UnityEngine;

namespace ObjectPooling
{
    public class PooledObject : MonoBehaviour
    {
        public virtual void ReturnToPool()
        {
           gameObject.SetActive(true);
        }
    }
}