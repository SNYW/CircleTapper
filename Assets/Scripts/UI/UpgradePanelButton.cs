using Core;
using DG.Tweening;
using Managers;
using UnityEngine;

namespace UI
{
    public class UpgradePanelButton : MonoBehaviour
    {
        private const float PulseScale = 1.25f;
        private const float PulseSeconds = 2f;

        public CanvasGroup availableUpgradePanel;
        private Vector3 _startPanelScale;
        private Tween _pulse;

        private void Awake()
        {
            _startPanelScale = availableUpgradePanel.transform.localScale;
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
            if (ServiceLocator.Get<UpgradeCatalog>().CanPurchaseAny()) return;

            availableUpgradePanel.alpha = 0;
            StopPulse();
        }

        private void OnUpgradePointAdded(object obj)
        {
            if (!ServiceLocator.Get<UpgradeCatalog>().CanPurchaseAny() || availableUpgradePanel.alpha != 0) return;

            StopPulse();

            availableUpgradePanel.alpha = 1;

            // Was ~40 lines of hand-rolled yoyo plus its own EaseInOutSine.
            _pulse = availableUpgradePanel.transform
                .DOScale(_startPanelScale * PulseScale, PulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private void StopPulse()
        {
            _pulse?.Kill();
            _pulse = null;
            availableUpgradePanel.transform.localScale = _startPanelScale;
        }

        private void OnDisable()
        {
            StopPulse();
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.UpgradePointAdded, OnUpgradePointAdded);
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.UpgradePointSpent, OnUpgradePointSpend);
        }
    }
}
