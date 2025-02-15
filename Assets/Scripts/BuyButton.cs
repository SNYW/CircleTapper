using System;
using System.Collections;
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
      _button.interactable = PurchaseManager.CanPurchase(_currentCost);
   }

   public void OnMouseDown()
   {
      if (!PurchaseManager.TryPurchaseItem(_currentCost)) return;

      var newObj = Instantiate(objectToBuy);
      GridManager.GetClosestCell(Vector2.zero).SetChildObject(newObj);
      _currentCost = cost * FindObjectsByType<BoardObject>(FindObjectsSortMode.None).Length;
      costText.text = _currentCost.ToString();
      SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, newObj);
   }
}