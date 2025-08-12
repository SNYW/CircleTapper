using Managers;
using UnityEngine;
using System.Collections;

namespace UI
{
    public class UpgradePanelButton : MonoBehaviour
    {
        public CanvasGroup availableUpgradePanel;
        private Vector3 _startPanelScale;
        private Coroutine _scaleRoutine;

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
            if (UpgradeManager.CanPurchaseAnyUpgrades()) return;

            availableUpgradePanel.alpha = 0;

            if (_scaleRoutine != null)
            {
                StopCoroutine(_scaleRoutine);
                _scaleRoutine = null;
            }

            availableUpgradePanel.transform.localScale = _startPanelScale;
        }

        private void OnUpgradePointAdded(object obj)
        {
            if (!UpgradeManager.CanPurchaseAnyUpgrades() || availableUpgradePanel.alpha != 0) return;

            if (_scaleRoutine != null)
            {
                StopCoroutine(_scaleRoutine);
                _scaleRoutine = null;
            }

            availableUpgradePanel.transform.localScale = _startPanelScale;
            availableUpgradePanel.alpha = 1;
            _scaleRoutine = StartCoroutine(ScaleYoyoRoutine(availableUpgradePanel.transform, _startPanelScale, _startPanelScale * 1.25f, 2f));
        }

        private IEnumerator ScaleYoyoRoutine(Transform target, Vector3 from, Vector3 to, float duration)
        {
            while (true)
            {
                yield return LerpScale(target, from, to, duration, EaseInOutSine);
                yield return LerpScale(target, to, from, duration, EaseInOutSine);
            }
        }

        private IEnumerator LerpScale(Transform target, Vector3 start, Vector3 end, float duration, System.Func<float, float> ease)
        {
            float time = 0f;
            while (time < duration)
            {
                float t = time / duration;
                target.localScale = Vector3.LerpUnclamped(start, end, ease(t));
                time += Time.deltaTime;
                yield return null;
            }
            target.localScale = end;
        }

        private float EaseInOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * t) - 1) / 2f;
        }

        private void OnDisable()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.UpgradePointAdded, OnUpgradePointAdded);
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.UpgradePointSpent, OnUpgradePointSpend);
        }
    }
}
