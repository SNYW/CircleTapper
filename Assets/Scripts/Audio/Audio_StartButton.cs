using UnityEngine;

public class Audio_StartButton : MonoBehaviour
{
    public FMODUnity.EventReference StartButtonSFX; //dean
    void StartButtonSFXPlay()
    {
        FMODUnity.RuntimeManager.PlayOneShotAttached(StartButtonSFX, gameObject); //dean
    }
}
