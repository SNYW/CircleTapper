using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Persistence;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static bool DEBUGMODE = false;

    public BoardObject defaultStartingObject;

    public List<Circle> circleLevels;
    public List<Square> squareLevels;
    
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
        SaveManager.Instance.Init(this);
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

    public void ResetOnLoad()
    {
        var objects = FindObjectsByType<BoardObject>(FindObjectsSortMode.None);
        foreach (var boardObject in objects)
        {
            Destroy(boardObject.gameObject);
        }
    }
}
