using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Economy;
using Managers;
using Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SystemEventManager;

namespace UI
{
    /// <summary>
    /// Progress towards the next upgrade point, and the button that claims it.
    /// <para>
    /// Claiming is the only moment in the game where the player is handed something for a
    /// deliberate choice, so it gets the loudest feedback the panel can give: the button asks to
    /// be pressed while it is claimable, and answers when it is.
    /// </para>
    /// </summary>
    public class ObjectiveTrackerPanel : MonoBehaviour
    {
        private const float SliderFillSeconds = 0.25f;
        private const float MinimumFill = 0.04f;

        private const float PulseScale = 1.08f;
        private const float PulseSeconds = 0.6f;

        private const float ClaimPunch = 0.35f;
        private const float ClaimPunchSeconds = 0.4f;
        private const float TextPunch = 0.25f;
        private const float TextPunchSeconds = 0.35f;

        /// <summary>Long enough for the claim punch to read before the panel covers it.</summary>
        private const float UpgradePanelDelaySeconds = 0.35f;

        public TMP_Text objectiveText;
        public Slider progressSlider;
        public Button claimButton;
        public GameObject allCompletePanel;

        [Tooltip("Opened after claiming, so the point that was just earned has somewhere to go.")]
        public GameObject upgradePanel;

        public FMODUnity.EventReference claimButtonSFX;

        private ObjectiveService _objectives;
        private CurrencyService _currency;
        private UpgradeCatalog _catalog;

        private Vector3 _buttonRestScale;
        private Vector3 _textRestScale;

        private Tween _pulse;
        private Tween _sliderFill;

        private bool _wasClaimable;

        private void Awake()
        {
            _buttonRestScale = claimButton.transform.localScale;
            _textRestScale = objectiveText.transform.localScale;
        }

        private void Start()
        {
            _objectives = ServiceLocator.Get<ObjectiveService>();
            _currency = ServiceLocator.Get<CurrencyService>();
            _catalog = ServiceLocator.Get<UpgradeCatalog>();

            _objectives.CurrentChanged += OnObjectiveAdvanced;
            _objectives.Progressed += OnProgressed;

            Subscribe(GameEvent.CurrencyAdded, OnStateChanged);
            Subscribe(GameEvent.CurrencySpent, OnStateChanged);
            Subscribe(GameEvent.UpgradePointSpent, OnStateChanged);
            Subscribe(GameEvent.GameLoaded, OnStateChanged);

            progressSlider.value = CurrentFill();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_objectives != null)
            {
                _objectives.CurrentChanged -= OnObjectiveAdvanced;
                _objectives.Progressed -= OnProgressed;
            }

            Unsubscribe(GameEvent.CurrencyAdded, OnStateChanged);
            Unsubscribe(GameEvent.CurrencySpent, OnStateChanged);
            Unsubscribe(GameEvent.UpgradePointSpent, OnStateChanged);
            Unsubscribe(GameEvent.GameLoaded, OnStateChanged);

            _pulse?.Kill();
            _sliderFill?.Kill();
        }

        private void OnStateChanged(object payload) => Refresh();

        private void OnProgressed() => Refresh();

        private void Refresh()
        {
            bool allComplete = _catalog.AllComplete();

            allCompletePanel.SetActive(allComplete);

            // The label comes from the objective itself, so a tutorial step reads "Merge two
            // circles" while the open-ended ones still read "Next Upgrade".
            objectiveText.text =
                $"{_objectives.Label}: {FormatNumber(_objectives.Progress)}/{FormatNumber(_objectives.Target)}";

            _sliderFill?.Kill();
            _sliderFill = progressSlider
                .DOValue(CurrentFill(), SliderFillSeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);

            bool claimable = !allComplete && _objectives.CanClaim;
            claimButton.interactable = claimable;

            if (claimable != _wasClaimable)
            {
                _wasClaimable = claimable;
                SetPulsing(claimable);
            }
        }

        private float CurrentFill()
            => Mathf.Max((float)_objectives.Progress / Mathf.Max(_objectives.Target, 1), MinimumFill);

        /// <summary>
        /// The button asks to be pressed while there is something to claim, matching the pulse the
        /// upgrade panel button already uses when upgrades are affordable.
        /// </summary>
        private void SetPulsing(bool pulsing)
        {
            _pulse?.Kill();
            _pulse = null;
            claimButton.transform.localScale = _buttonRestScale;

            if (!pulsing) return;

            _pulse = claimButton.transform
                .DOScale(_buttonRestScale * PulseScale, PulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        /// <summary>The objective number moved on, so draw the eye to the new target.</summary>
        private void OnObjectiveAdvanced(int current)
        {
            objectiveText.transform.DOKill();
            objectiveText.transform.localScale = _textRestScale;
            objectiveText.transform
                .DOPunchScale(_textRestScale * TextPunch, TextPunchSeconds)
                .SetLink(gameObject);
        }

        public void ClaimCurrentObjective()
        {
            // Once every upgrade is maxed there is nothing left to award, so stop taking points.
            if (_catalog.AllComplete()) return;
            if (!_objectives.TryClaim()) return;

            // Claiming spends the points, so the pulse has already stopped by the time this runs.
            claimButton.transform.DOKill();
            claimButton.transform.localScale = _buttonRestScale;
            claimButton.transform
                .DOPunchScale(_buttonRestScale * ClaimPunch, ClaimPunchSeconds)
                .SetLink(gameObject);

            FMODUnity.RuntimeManager.PlayOneShotAttached(claimButtonSFX, gameObject);

            OpenUpgradePanelShortly(this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// Claiming hands the player an upgrade point, so it shows them where to spend it. Delayed
        /// by a beat so the button's punch is seen rather than immediately covered.
        /// </summary>
        private async UniTaskVoid OpenUpgradePanelShortly(CancellationToken token)
        {
            if (upgradePanel == null || upgradePanel.activeSelf) return;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(UpgradePanelDelaySeconds), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (upgradePanel != null) upgradePanel.SetActive(true);
        }

        private static string FormatNumber(long number)
        {
            if (number >= 1_000_000)
                return number % 1_000_000 == 0
                    ? number / 1_000_000 + "m"
                    : (number / 1_000_000f).ToString("0.0") + "m";

            if (number >= 1_000)
                return number % 1_000 == 0
                    ? number / 1_000 + "k"
                    : (number / 1_000f).ToString("0.0") + "k";

            return number.ToString();
        }
    }
}
