using System.Collections;
using System.Collections.Generic;
using ObjectPooling;
using UnityEngine;
using static SoundIndex;

public static class SoundManager
{
   private const int MaxSounds = 30;
   private static SoundIndex _soundIndex;

   public static void Init()
   {
      _soundIndex = Resources.Load<SoundIndex>("Data/Audio/Sound Index");
   }

   public static void PlaySound(SoundName name, float pitch = 1, float volume = 1)
   {
      PlayPooledSound(volume, pitch, name);
   }

   public static void PlaySound(SoundName name, Vector2 pitchRandom, float volume)
   {
      var pitch = Random.Range(pitchRandom.x, pitchRandom.y);
      PlayPooledSound(volume, pitch, name);
   }

   private static void PlayPooledSound(float volume, float pitch, SoundName name)
   {
      var pool = ObjectPoolManager.GetPool(ObjectPool.ObjectPoolName.SoundPlayer);
      if(pool.GetActiveAmount() > MaxSounds) return;
      
      var player = pool.GetPooledObject().GetComponent<PooledSoundPlayer>();
      player.gameObject.SetActive(true);
      player.Init(volume, pitch, _soundIndex.GetSound(name));
   }
}
