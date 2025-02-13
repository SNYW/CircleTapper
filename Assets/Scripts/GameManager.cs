using DG.Tweening;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(transform.parent.gameObject);
        DOTween.Init();
        SystemEventManager.Init();
        PurchaseManager.Init();
    }

    private void OnDisable()
    {
        PurchaseManager.Dispose();
    }
}
