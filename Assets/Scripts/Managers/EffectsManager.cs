using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Managers
{
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager Instance { get; private set; }

        public enum EffectType
        {
            Spawn,
            Deletion
        }

        [System.Serializable]
        public struct EffectMapping
        {
            public EffectType type;
            public GameObject prefab;
        }

        public List<EffectMapping> effectMappings;

        private readonly Dictionary<EffectType, GameObject> _effectDict = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            foreach (var mapping in effectMappings.Where(mapping => !_effectDict.ContainsKey(mapping.type)))
            {
                _effectDict[mapping.type] = mapping.prefab;
            }
        }

        public void SpawnEffect(EffectType type, Vector3 position)
        {
            if (_effectDict.TryGetValue(type, out var prefab) && prefab != null)
            {
                var effect = Instantiate(prefab, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            else
            {
                Debug.LogWarning($"Effect type {type} not found or prefab is null.");
            }
        }
    }
}