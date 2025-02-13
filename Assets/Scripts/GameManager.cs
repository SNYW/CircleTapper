using DG.Tweening;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static bool DEBUGMODE = false;
    private void Awake()
    {
        DontDestroyOnLoad(transform.parent.gameObject);
        DOTween.Init();
        SystemEventManager.Init();
        PurchaseManager.Init();
    }

    public void ToggleDebug()
    {
        DEBUGMODE = !DEBUGMODE;
    }

    private void OnDisable()
    {
        PurchaseManager.Dispose();
    }
}
