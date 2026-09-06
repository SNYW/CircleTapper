using Core;
using Persistence;
using UnityEngine;

public class DeleteSaveButton : MonoBehaviour
{
    public FMODUnity.EventReference DeleteSaveSFX; //audio 
    public void DeleteSave()
    {
        ServiceLocator.Get<SaveService>().DeleteSave();
        FMODUnity.RuntimeManager.PlayOneShotAttached(DeleteSaveSFX, gameObject); //audio
    }
}
