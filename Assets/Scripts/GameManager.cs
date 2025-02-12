using System;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(transform.parent.gameObject);
        SystemEventManager.Init();
        PurchaseManager.Init();
    }

    private void OnDisable()
    {
        GridManager.Dispose();
        PurchaseManager.Dispose();
    }
}
