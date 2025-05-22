using System;
using System.Collections;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyButton : MonoBehaviour
{
   public int cost;
   public BoardObject objectToBuy;
   public TMP_Text costText;

   private int _currentCost;
   private Button _button;

   public FMODUnity.EventReference BuyButtonSFX; //audio 


    private void Awake()
   {
      _currentCost = cost;
      costText.text = cost.ToString();
      _button = GetComponent<Button>();
   }


   private void LateUpdate()
   {
      _currentCost = cost * FindObjectsByType<BoardObject>(FindObjectsSortMode.None).Length;
      costText.text = _currentCost.ToString();
   }

   private void Update()
   {
      if (GameManager.DEBUGMODE)
      {
         _button.interactable = true;
         return;
      }
      _button.interactable = PurchaseManager.CanPurchase(_currentCost);
   }

   public void OnMouseDown()
   {
      if (GameManager.DEBUGMODE)
      {
         var newBoardObject = Instantiate(objectToBuy);
         GridManager.GetClosestCell(Vector2.zero).SetChildObject(newBoardObject);
         newBoardObject.Init();
         _currentCost = cost * FindObjectsByType<BoardObject>(FindObjectsSortMode.None).Length;
         costText.text = _currentCost.ToString();
         SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, newBoardObject);
         return;
      }
      
      var newCell = GridManager.GetClosestCell(Vector2.zero);
      if (newCell == null || !PurchaseManager.TryPurchaseItem(_currentCost)) return;

      var newObj = Instantiate(objectToBuy);
      newCell.SetChildObject(newObj);
      newObj.Init();
      _currentCost = cost * FindObjectsByType<BoardObject>(FindObjectsSortMode.None).Length;
      costText.text = _currentCost.ToString();
      SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, newObj);
      FMODUnity.RuntimeManager.PlayOneShotAttached(BuyButtonSFX, gameObject); //audio
    }
}