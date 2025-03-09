using Persistence;
using UnityEngine;

public class DeleteSaveButton : MonoBehaviour
{
    public void DeleteSave()
    {
        SaveManager.Instance.DeleteSave();
    }
}
