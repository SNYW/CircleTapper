using DG.Tweening;
using Managers;
using UnityEngine;

namespace UI
{
    public class UpgradePanelButton : MonoBehaviour
    {
        public CanvasGroup availableUpgradePanel;
        private void Awake()
        {
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.UpgradePointAdded, OnUpgradePointAdded);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.UpgradePointSpent, OnUpgradePointSpend);
        }

        private void Start()
        {
            availableUpgradePanel.alpha = 0;
            OnUpgradePointAdded(null);
        }

        private void OnUpgradePointSpend(object obj)
        {
            if (UpgradeManager.CanPurchaseAnyUpgrades()) return;
            availableUpgradePanel.alpha = 0;
        }

        private void OnUpgradePointAdded(object obj)
        {
            if (UpgradeManager.CanPurchaseAnyUpgrades() && availableUpgradePanel.alpha == 0)
            {
                availableUpgradePanel.transform
                    .DOScale(availableUpgradePanel.transform.localScale * 1.05f, 2f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
                availableUpgradePanel.DOFade(1, 0.8f);
            }
        }

        private void OnDisable()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.UpgradePointAdded, OnUpgradePointAdded);
        }
    }
}