using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    public class AudioSettingsUI : MonoBehaviour
    {
        private MixManager _mixManager;

        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;

        private void Start()
        {
            _mixManager = FindAnyObjectByType<MixManager>();

            if (_mixManager == null)
            {
                Debug.LogError("MixManager not found in the scene!");
                return;
            }

            masterSlider.value = _mixManager.masterVolume;
            musicSlider.value = _mixManager.musicVolume;
            sfxSlider.value = _mixManager.sfxVolume;

            masterSlider.onValueChanged.AddListener(_mixManager.SetMasterVolume);
            musicSlider.onValueChanged.AddListener(_mixManager.SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(_mixManager.SetSFXVolume);
        }
    }
}
