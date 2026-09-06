using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Persistence
{
    /// <summary>
    /// Owns the save file and the in-memory <see cref="GameData"/>. Pure persistence — it knows
    /// nothing about scenes, prefabs or the board. Reconstructing a session from the loaded data
    /// is the job of a scene-side component.
    /// <para>
    /// Writes are coalesced. Callers mark the save dirty as often as they like; the actual
    /// serialize-and-write happens on a timer, on pause, and on anything explicitly flushed.
    /// </para>
    /// </summary>
    public class SaveService : IGameService, IAsyncInitializable, ITickable, IServiceDisposable,
        IApplicationLifecycle
    {
        /// <summary>Bump when the shape of <see cref="GameData"/> changes.</summary>
        public const int CurrentSaveVersion = 1;

        private const string SaveFileName = "CTSave";
        private const float FlushIntervalSeconds = 5f;

        private readonly IDataService _dataService;
        private readonly Dictionary<Vector2Int, BoardObjectSaveData> _boardObjects = new();

        private bool _isDirty;
        private bool _isLoaded;
        private float _secondsSinceFlush;

        public SaveService(IDataService dataService = null)
        {
            _dataService = dataService ?? new FileDataService(new JsonSerializer());
        }

        public GameData Data { get; private set; } = NewGameData();

        /// <summary>True when there was no usable save and the player is starting fresh.</summary>
        public bool IsNewGame { get; private set; }

        /// <summary>
        /// Live view for lookups. Do not enumerate this while doing anything that writes back to
        /// the save — spawning a board object does exactly that. Use
        /// <see cref="SnapshotBoardObjects"/> to iterate.
        /// </summary>
        public IReadOnlyDictionary<Vector2Int, BoardObjectSaveData> BoardObjects => _boardObjects;

        /// <summary>A detached copy, safe to iterate while the save is being written to.</summary>
        public List<BoardObjectSaveData> SnapshotBoardObjects() => new(_boardObjects.Values);

        /// <summary>Raised once the save has been read and is safe to consume.</summary>
        public event Action Loaded;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Data = ReadFromDisk();
            RebuildBoardLookup();

            _isLoaded = true;
            Loaded?.Invoke();

            return UniTask.CompletedTask;
        }

        private GameData ReadFromDisk()
        {
            if (!_dataService.Exists(SaveFileName))
            {
                IsNewGame = true;
                return NewGameData();
            }

            GameData loaded;
            try
            {
                loaded = _dataService.Load(SaveFileName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Save file could not be read, trying the backup.\n{exception}");

                if (_dataService.TryLoadBackup(SaveFileName, out GameData backup))
                {
                    Debug.LogWarning("Recovered the previous save from backup.");
                    loaded = backup;
                }
                else
                {
                    // Preserve rather than delete — a player's progress may still be recoverable.
                    _dataService.QuarantineCorrupt(SaveFileName);
                    IsNewGame = true;
                    return NewGameData();
                }
            }

            if (loaded.saveVersion != CurrentSaveVersion)
            {
                // Beta behaviour. This must become a migration before public launch.
                Debug.LogWarning(
                    $"Save version {loaded.saveVersion} does not match {CurrentSaveVersion}; starting fresh.");
                IsNewGame = true;
                return NewGameData();
            }

            if (loaded.unlockedCells == null || loaded.unlockedCells.Count == 0)
            {
                Debug.LogWarning("Save has no unlocked cells, which is not a playable state; starting fresh.");
                IsNewGame = true;
                return NewGameData();
            }

            return loaded;
        }

        /// <summary>Discards the loaded session and starts from a clean slate, in memory.</summary>
        public void ResetToNewGame()
        {
            Data = NewGameData();
            _boardObjects.Clear();
            IsNewGame = true;
            MarkDirty();

            // A reset is a new session; whoever builds the board needs to rebuild it.
            Loaded?.Invoke();
        }

        /// <summary>Removes the save from disk entirely, then resets in memory.</summary>
        public void DeleteSave()
        {
            _dataService.Delete(SaveFileName);
            ResetToNewGame();
        }

        public void SetBoardObject(Vector2Int position, BoardObjectSaveData data)
        {
            _boardObjects[position] = data;
            MarkDirty();
        }

        public void RemoveBoardObject(Vector2Int position)
        {
            if (_boardObjects.Remove(position)) MarkDirty();
        }

        public bool IsCellUnlocked(Vector2Int position) => Data.unlockedCells.Contains(position);

        /// <summary>Records a newly unlocked cell. False if it was already unlocked.</summary>
        public bool TryRecordUnlockedCell(Vector2Int position)
        {
            if (Data.unlockedCells.Contains(position)) return false;

            Data.unlockedCells.Add(position);
            Flush(); // Progression is worth writing immediately.
            return true;
        }

        public void SaveUpgrade(UpgradeSaveObject upgrade)
        {
            UpgradeSaveObject existing = Data.upgrades.Find(u => u.upgradeName == upgrade.upgradeName);
            if (existing != null) existing.currentLevel = upgrade.currentLevel;
            else Data.upgrades.Add(upgrade);

            Flush(); // A purchase must never be lost.
        }

        /// <summary>Queues a write. Cheap — safe to call from hot paths.</summary>
        public void MarkDirty() => _isDirty = true;

        public void Tick(float deltaTime)
        {
            if (!_isDirty) return;

            _secondsSinceFlush += deltaTime;
            if (_secondsSinceFlush < FlushIntervalSeconds) return;

            Flush();
        }

        /// <summary>Writes immediately if there is anything to write.</summary>
        public void Flush()
        {
            _secondsSinceFlush = 0f;

            if (!_isLoaded) return;

            _isDirty = false;

            try
            {
                Data.saveVersion = CurrentSaveVersion;
                Data.boardObjects = new List<BoardObjectSaveData>(_boardObjects.Values);

                _dataService.Save(SaveFileName, Data);
            }
            catch (Exception exception)
            {
                // A failed write must not take the game down. The previous save is still intact.
                Debug.LogError($"Failed to write the save file.\n{exception}");
            }
        }

        public void OnApplicationPaused(bool paused)
        {
            if (paused) Flush();
        }

        public void OnApplicationQuitting() => Flush();

        public void DisposeService()
        {
            Flush();
            Loaded = null;
        }

        private void RebuildBoardLookup()
        {
            _boardObjects.Clear();

            foreach (BoardObjectSaveData boardObject in Data.boardObjects)
            {
                _boardObjects[new Vector2Int(boardObject.xPosition, boardObject.yPosition)] = boardObject;
            }
        }

        private static GameData NewGameData() => new()
        {
            saveVersion = CurrentSaveVersion,
            currentPoints = 0,
            currentUpgradePoints = 0,
            currentObjective = 1,
            boardObjects = new List<BoardObjectSaveData>(),
            unlockedCells = new List<Vector2Int>(),
            upgrades = new List<UpgradeSaveObject>()
        };
    }
}
