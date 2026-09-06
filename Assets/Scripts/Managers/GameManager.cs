using Economy;
using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using ObjectPooling;
using Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    /// <summary>
    /// Scene-side owner of a gameplay session. <see cref="SaveService"/> holds the data; this
    /// turns that data into an actual board — spawning objects, unlocking cells, framing the
    /// camera — and pushes state that still lives in the static managers back into the save.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static bool DEBUGMODE;

        public int passiveBonus;
        public int passiveUpgradeBonus;
        public BoardObject defaultStartingObject;
        public InWorldGridManager gridManager;

        public List<Circle> circleLevels;
        public List<Square> squareLevels;
        public List<Hex> hexLevels;

        private const int GameplaySceneIndex = 1;
        private const float PassiveIncomeIntervalSeconds = 1f;

        private SaveService _save;
        private CurrencyService _currency;
        private float _secondsSincePayout;
        private bool _sessionLoaded;

        private void Awake()
        {
#if !UNITY_EDITOR
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif

            DontDestroyOnLoad(transform.parent.gameObject);
            ObjectPoolManager.InitPools();
            DOTween.Init();
            SystemEventManager.Init();

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoad;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoad;

            if (_currency != null)
            {
                _currency.PointsChanged -= OnPointsChanged;
                _currency.UpgradePointsChanged -= OnUpgradePointsChanged;
            }

            if (_save != null) _save.Loaded -= BuildSession;
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.buildIndex != GameplaySceneIndex) return;

            gridManager.gameObject.SetActive(true);
            LoadSessionWhenReady(this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// The save is read during bootstrap, which finishes asynchronously and may well land
        /// after this scene has loaded. Wait for it rather than racing it.
        /// </summary>
        private async UniTaskVoid LoadSessionWhenReady(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => GameBootstrapper.IsReady, cancellationToken: cancellationToken);

            _save = ServiceLocator.Get<SaveService>();

            _currency = ServiceLocator.Get<CurrencyService>();
            _currency.PointsChanged += OnPointsChanged;
            _currency.UpgradePointsChanged += OnUpgradePointsChanged;

            BuildSession();

            // Fires again if the save is reset at runtime, e.g. the delete-save button.
            _save.Loaded += BuildSession;
        }

        /// <summary>Turns the loaded <see cref="GameData"/> into a playable board.</summary>
        private void BuildSession()
        {
            ClearBoard();


            if (_save.IsNewGame) StartFreshBoard();
            else RestoreBoard();

            FindAnyObjectByType<CameraZoomController>().OnGameplayStart();

            _sessionLoaded = true;
            SystemEventManager.Send(SystemEventManager.GameEvent.GameLoaded, _save.SnapshotBoardObjects());
        }

        private void RestoreBoard()
        {
            foreach (Vector2Int cellPosition in _save.Data.unlockedCells)
            {
                GridCell cell = GridManager.GetGridCell(cellPosition, true);
                if (cell != null) cell.Unlock(false);
            }

            // Snapshot first: spawning writes each object straight back into the save, so
            // iterating the live collection would mutate it mid-enumeration.
            foreach (BoardObjectSaveData boardObject in _save.SnapshotBoardObjects())
            {
                try
                {
                    Spawn(boardObject);
                }
                catch (Exception exception)
                {
                    // One unspawnable object must not cost the player the rest of their board.
                    Debug.LogError(
                        $"Failed to restore {boardObject.type} at " +
                        $"({boardObject.xPosition},{boardObject.yPosition}).\n{exception}");
                }
            }
        }

        private void StartFreshBoard()
        {
            GridManager.ResetCells();

            GridCell startingCell = GridManager.GetClosestCell(Vector2Int.zero, true, true);
            if (startingCell == null)
            {
                Debug.LogError("No locked cell available to start a new game on.");
                return;
            }

            if (_save.TryRecordUnlockedCell(startingCell.gridPosition)) startingCell.Unlock();

            BoardObject starting = Instantiate(defaultStartingObject);
            startingCell.SetChildObject(starting);
            starting.Init();
        }

        private void Spawn(BoardObjectSaveData data)
        {
            if (!Enum.TryParse(data.type, out BoardObjectType type))
            {
                Debug.LogError($"Save contains an unknown board object type '{data.type}'.");
                return;
            }

            BoardObject prefab = type switch
            {
                BoardObjectType.Circle => LevelOrNull<Circle>(circleLevels, data.level),
                BoardObjectType.Square => LevelOrNull<Square>(squareLevels, data.level),
                BoardObjectType.Hex => LevelOrNull<Hex>(hexLevels, data.level),
                _ => null
            };

            if (prefab == null)
            {
                Debug.LogError($"No prefab for {data.type} level {data.level}; skipping it.");
                return;
            }

            Instantiate(prefab).FromSaveData(data);
        }

        private static BoardObject LevelOrNull<T>(List<T> levels, int level) where T : BoardObject
            => levels != null && level >= 0 && level < levels.Count ? levels[level] : null;

        private void ClearBoard()
        {
            foreach (BoardObject boardObject in FindObjectsByType<BoardObject>(FindObjectsSortMode.None))
            {
                Destroy(boardObject.gameObject);
            }
        }

        /// <summary>
        /// Bridges the currency service's own events onto the old event bus, for UI that has not
        /// been converted yet. Delete once nothing subscribes to the currency GameEvents.
        /// </summary>
        private static void OnPointsChanged(long previous, long current)
        {
            SystemEventManager.Send(
                current > previous
                    ? SystemEventManager.GameEvent.CurrencyAdded
                    : SystemEventManager.GameEvent.CurrencySpent,
                current);
        }

        private static void OnUpgradePointsChanged(long previous, long current)
        {
            SystemEventManager.Send(
                current > previous
                    ? SystemEventManager.GameEvent.UpgradePointAdded
                    : SystemEventManager.GameEvent.UpgradePointSpent,
                current);
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Space)) ToggleDebug();
#endif
            if (!_sessionLoaded) return;

            _secondsSincePayout += Time.deltaTime;
            if (_secondsSincePayout < PassiveIncomeIntervalSeconds) return;

            _secondsSincePayout = 0f;
            _currency.AddPoints(GridManager.GetPassiveIncomeAmount() + passiveBonus);
            _currency.AddUpgradePoints(passiveUpgradeBonus);
        }

        public void ToggleDebug() => DEBUGMODE = !DEBUGMODE;
    }
}
