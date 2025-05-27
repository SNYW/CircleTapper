using Gameplay;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UpgradeButton : MonoBehaviour
    {
        public UpgradeDefinition definition;
        public TMP_Text costText;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public Button button;

        private void Awake()
        {
            nameText.text = definition.upgradeName;
        }

        private void OnEnable()
        {
            OnUpgradePointSpent(null);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.UpgradePointSpent, OnUpgradePointSpent);
        }

        private void OnUpgradePointSpent(object obj)
        {
            button.interactable = definition.CanPurchase();
            costText.text = definition.IsMaxed() ? "MAX" : definition.GetPurchasePrice().ToString();
            levelText.text = definition.GetLevelInfo();
        }

        public void OnButtonDown()
        {
            definition.OnLevelUp();
            costText.text = definition.GetPurchasePrice().ToString();
            SystemEventManager.Send(SystemEventManager.GameEvent.UpgradePointSpent, PurchaseManager.GetCurrentUpgradePoints());
        }

        private void OnDisable()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.UpgradePointSpent, OnUpgradePointSpent);
        }
    }
}