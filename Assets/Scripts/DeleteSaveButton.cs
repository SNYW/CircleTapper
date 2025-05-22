using Persistence;
using UnityEngine;

public class DeleteSaveButton : MonoBehaviour
{
    public FMODUnity.EventReference DeleteSaveSFX; //audio 
    public void DeleteSave()
    {
        SaveManager.Instance.DeleteSave();
        FMODUnity.RuntimeManager.PlayOneShotAttached(DeleteSaveSFX, gameObject); //audio
    }
}
