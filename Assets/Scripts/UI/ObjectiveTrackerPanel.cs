using System;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ObjectiveTrackerPanel : MonoBehaviour
    {
        public TMP_Text objectiveText;
        public Slider progressSlider;
        public Button claimButton;
        public GameObject allCompletePanel;

        public FMODUnity.EventReference claimButtonSFX;

        public void Init()
        {
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencyAdded, OnObjectiveUpdated);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencySpent, OnObjectiveUpdated);
            claimButton.interactable = false;
            allCompletePanel.SetActive(ObjectiveManager.AllObjectivesComplete);
            OnObjectiveProgressed();
        }

        private void OnObjectiveUpdated(object obj)
        {
            OnObjectiveProgressed();
        }

        private void OnObjectiveProgressed()
        {
            objectiveText.text = $"Next Upgrade: {FormatNumber(PurchaseManager.GetCurrentCurrency())}/{FormatNumber(ObjectiveManager.GetCurrentObjectiveCost())}";
            progressSlider.value = Mathf.Max((float)PurchaseManager.GetCurrentCurrency()/ObjectiveManager.GetCurrentObjectiveCost(), 0.04f);
            claimButton.interactable = ObjectiveManager.CanClaimObjective();
        }

        private void Update()
        {
            allCompletePanel.SetActive(ObjectiveManager.AllObjectivesComplete);
        }

        public void ClaimCurrentObjective()
        {
            if (!ObjectiveManager.CanClaimObjective()) return;
            
            ObjectiveManager.ClaimObjective();
            allCompletePanel.SetActive(ObjectiveManager.AllObjectivesComplete);
            FMODUnity.RuntimeManager.PlayOneShotAttached(claimButtonSFX, gameObject);
        }

        private static string FormatNumber(int number)
        {
            if (number >= 1_000_000)
                return (number % 1_000_000 == 0) ? (number / 1_000_000) + "m" : (number / 1_000_000f).ToString("0.0") + "m";
            if (number >= 1_000)
                return (number % 1_000 == 0) ? (number / 1_000) + "k" : (number / 1_000f).ToString("0.0") + "k";
    
            return number.ToString();
        }
        
        private static string FormatNumber(long number)
        {
            if (number >= 1_000_000)
                return (number % 1_000_000 == 0) ? (number / 1_000_000) + "m" : (number / 1_000_000f).ToString("0.0") + "m";
            if (number >= 1_000)
                return (number % 1_000 == 0) ? (number / 1_000) + "k" : (number / 1_000f).ToString("0.0") + "k";
    
            return number.ToString();
        }

        private void OnDisable()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencyAdded, OnObjectiveUpdated);
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencySpent, OnObjectiveUpdated);
        }
    }
}