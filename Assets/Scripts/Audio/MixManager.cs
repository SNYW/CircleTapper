using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MixManager : MonoBehaviour
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // In case this isn’t already here

        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");

        LoadVolumeSettings(); // Load saved settings when game starts
        ApplyVolumes();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        masterBus.setVolume(value);
        PlayerPrefs.SetFloat("Volume_Master", value);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        musicBus.setVolume(value);
        PlayerPrefs.SetFloat("Volume_Music", value);
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        sfxBus.setVolume(value);
        PlayerPrefs.SetFloat("Volume_SFX", value);
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("Volume_Master", 1f);
        musicVolume = PlayerPrefs.GetFloat("Volume_Music", 1f);
        sfxVolume = PlayerPrefs.GetFloat("Volume_SFX", 1f);
    }

    private void ApplyVolumes()
    {
        masterBus.setVolume(masterVolume);
        musicBus.setVolume(musicVolume);
        sfxBus.setVolume(sfxVolume);
    }
}
