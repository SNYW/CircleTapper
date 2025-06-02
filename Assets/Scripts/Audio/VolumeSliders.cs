using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class AudioSettingsUI : MonoBehaviour
{
    private MixManager mixManager;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // Look for MixManager in the scene (including DontDestroyOnLoad objects)
        mixManager = FindObjectOfType<MixManager>();

        if (mixManager == null)
        {
            Debug.LogError("MixManager not found in the scene!");
            return;
        }

        masterSlider.value = mixManager.masterVolume;
        musicSlider.value = mixManager.musicVolume;
        sfxSlider.value = mixManager.sfxVolume;

        masterSlider.onValueChanged.AddListener(mixManager.SetMasterVolume);
        musicSlider.onValueChanged.AddListener(mixManager.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(mixManager.SetSFXVolume);
    }
}
