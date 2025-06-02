using Managers;
using Objectives;
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

        private Objective targetObjective;

        public FMODUnity.EventReference claimButtonSFX;

        private void OnEnable()
        {
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.ObjectiveUpdated, OnObjectiveUpdated);
            claimButton.interactable = false;
            allCompletePanel.SetActive(ObjectiveManager.AllObjectivesComplete);
        }

        private void OnObjectiveUpdated(object obj)
        {
            if(obj is not Objective currentObjective) return;
            if (targetObjective != null) targetObjective.OnProgressed -= OnObjectiveProgressed;
            
            targetObjective = currentObjective;
            targetObjective.OnProgressed += OnObjectiveProgressed;
            OnObjectiveProgressed(-1);
        }

        private void OnObjectiveProgressed(int obj)
        {
            objectiveText.text = $"{targetObjective.GetDisplayText()}: {FormatNumber(targetObjective.current)}/{FormatNumber(targetObjective.amount)}";
            progressSlider.value = Mathf.Max(targetObjective.Percentage, 0.04f);
            claimButton.interactable = targetObjective.Claimable;
        }

        public void ClaimCurrentObjective()
        {
            if(targetObjective.Claimable) ObjectiveManager.ClaimObjective();
            allCompletePanel.SetActive(ObjectiveManager.AllObjectivesComplete);
            FMODUnity.RuntimeManager.PlayOneShotAttached(claimButtonSFX, gameObject); //audio
        }

        private static string FormatNumber(int number)
        {
            if (number >= 1_000_000)
                return (number % 1_000_000 == 0) ? (number / 1_000_000) + "m" : (number / 1_000_000f).ToString("0.0") + "m";
            if (number >= 1_000)
                return (number % 1_000 == 0) ? (number / 1_000) + "k" : (number / 1_000f).ToString("0.0") + "k";
    
            return number.ToString();
        }


        private void OnDisable()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.ObjectiveUpdated, OnObjectiveUpdated);
        }
    }
}