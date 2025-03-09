using ObjectPooling;
using UnityEngine;

public class PooledSoundPlayer : PooledObject
{
    private AudioSource _audioSource;
    private bool _hasPlayed;
    
    private void OnEnable()
    {
        GetAudioSource();
        _hasPlayed = false;
    }

    private void GetAudioSource()
    {
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
    }

    private void LateUpdate()
    {
        if(_hasPlayed && !_audioSource.isPlaying) ReturnToPool();
    }

    public void Init(float volume, float pitch, AudioClip clip)
    {
        GetAudioSource();
        _audioSource.volume = volume;
        _audioSource.pitch = pitch;
        _audioSource.clip = clip;
        _audioSource.Play();
        _hasPlayed = true;
    }

    private void OnDisable()
    {
        _hasPlayed = false;
    }
}
