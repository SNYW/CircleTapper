using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static bool DEBUGMODE = false;

    public BoardObject defaultStartingObject;
    private void Awake()
    {
        DontDestroyOnLoad(transform.parent.gameObject);
        DOTween.Init();
        SystemEventManager.Init();
        PurchaseManager.Init();

        StartCoroutine(GivePassiveIncome());
    }

    private void Start()
    {
        GridManager.GetClosestCell(Vector2.zero).SetChildObject(Instantiate(defaultStartingObject));
    }

    private IEnumerator GivePassiveIncome()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(1);
            var boardItems = FindObjectsByType<BoardObject>(FindObjectsSortMode.None);
            SystemEventManager.Send(SystemEventManager.GameEvent.CurrencyAdded, boardItems.Length);
        }
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
