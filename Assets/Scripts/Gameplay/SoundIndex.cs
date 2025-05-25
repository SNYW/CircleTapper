using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

[CreateAssetMenu(fileName = "sound index", menuName = "Game Data/ Sound Index")]
public class SoundIndex : ScriptableObject
{
    
    public enum SoundName
    {
        CircleTap,
        CircleComplete,
        OrbSpawn,
        OrbDespawn
    }

    public List<AudioClip> circleTap;

    public AudioClip GetSound(SoundName soundName)
    {
        return soundName switch
        {
            SoundName.CircleTap => circleTap.GetRandom(),
            _ => throw new ArgumentOutOfRangeException(nameof(soundName), soundName, null)
        };
    }
}
