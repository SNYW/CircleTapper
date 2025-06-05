using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Audio
{
    public class MixManager : MonoBehaviour
    {
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;

        private Bus _masterBus;
        private Bus _musicBus;
        private Bus _sfxBus;

        private void Awake()
        {
            _masterBus = RuntimeManager.GetBus("bus:/");
            _musicBus = RuntimeManager.GetBus("bus:/Music");
            _sfxBus = RuntimeManager.GetBus("bus:/SFX");

            LoadVolumeSettings();
            ApplyVolumes();
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = value;
            _masterBus.setVolume(value);
            PlayerPrefs.SetFloat("Volume_Master", value);
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = value;
            _musicBus.setVolume(value);
            PlayerPrefs.SetFloat("Volume_Music", value);
        }

        public void SetSFXVolume(float value)
        {
            sfxVolume = value;
            _sfxBus.setVolume(value);
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
            _masterBus.setVolume(masterVolume);
            _musicBus.setVolume(musicVolume);
            _sfxBus.setVolume(sfxVolume);
        }
    }
}
