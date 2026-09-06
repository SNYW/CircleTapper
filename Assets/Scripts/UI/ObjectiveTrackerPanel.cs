using Progression;
using Economy;
using Core;
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
            claimButton.interactable = false;
            OnObjectiveProgressed();
        }

        private void OnObjectiveProgressed()
        {
            ObjectiveService objectives = ServiceLocator.Get<ObjectiveService>();
            bool allComplete = ServiceLocator.Get<UpgradeCatalog>().AllComplete();
            long points = ServiceLocator.Get<CurrencyService>().Points;

            allCompletePanel.SetActive(allComplete);
            objectiveText.text = $"Next Upgrade: {FormatNumber(points)}/{FormatNumber(objectives.CurrentCost)}";
            progressSlider.value = Mathf.Max((float)points / objectives.CurrentCost, 0.04f);
            claimButton.interactable = !allComplete && objectives.CanClaim;
        }

        private void Update()
        {
           OnObjectiveProgressed();
        }

        public void ClaimCurrentObjective()
        {
            // Once every upgrade is maxed there is nothing left to award, so stop taking points.
            if (ServiceLocator.Get<UpgradeCatalog>().AllComplete()) return;
            if (!ServiceLocator.Get<ObjectiveService>().TryClaim()) return;

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
    }
}