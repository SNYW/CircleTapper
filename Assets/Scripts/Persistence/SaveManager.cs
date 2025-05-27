using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Managers;
using UnityEditor;
using UnityEngine;

namespace Persistence
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance;
        [SerializeField] public GameData gameData;
        private IDataService _dataService;
        private const string SaveFileName = "CTSave";

        private Dictionary<Vector2Int, BoardObjectSaveData> _activeSaveData;
        private GameManager _gameManager;

        private bool _isLoaded = false;

        private void Awake()
        {
            if (Instance == this) return;

            if (Instance != null) Destroy(Instance.gameObject);

            Instance = this;
        }
        
        public void Init(GameManager gameManager)
        {
            _gameManager = gameManager;
            _gameManager.ResetOnLoad();
            _dataService = new FileDataService(new JsonSerializer());
            LoadGame(true);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencyAdded, OnSaveChanged);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencySpent, OnSaveChanged);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.ObjectiveUpdated, OnSaveChanged);
            _isLoaded = true;
        }

        public void SaveGame()
        {
            if(_isLoaded)
                _dataService.Save(SaveFileName, gameData);
        } 

        public void LoadGame(bool spawnObjects = false)
        {
            gameData = new GameData();
            _activeSaveData = new Dictionary<Vector2Int, BoardObjectSaveData>();
            
            try
            {
                gameData = _dataService.Load(SaveFileName);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                ResetSave();
                return;
            }
            
            if(gameData.unlockedCells.Count == 0) ResetSave();
            
            PurchaseManager.OnGameLoad(gameData);
            ObjectiveManager.OnGameLoad(gameData);
            UpgradeManager.OnGameLoad(gameData);

            foreach (var cellPos in gameData.unlockedCells)
            {
                var gameCell = GridManager.GetGridCell(cellPos);
                gameCell.Unlock(false);
            }
            
            foreach (var boardObject in gameData.boardObjects)
            {
                _activeSaveData.Add(new Vector2Int(boardObject.xPosition, boardObject.yPosition), boardObject);

                if (!spawnObjects) continue;

                var type = Enum.Parse<BoardObjectType>(boardObject.type);
                switch (type)
                {
                    case  BoardObjectType.Circle:
                        Instantiate(_gameManager.circleLevels[boardObject.level]).FromSaveData(boardObject);
                        break;
                    case  BoardObjectType.Square:
                        Instantiate(_gameManager.squareLevels[boardObject.level]).FromSaveData(boardObject);
                        break;
                    case BoardObjectType.Hex:
                        Instantiate(_gameManager.hexLevels[boardObject.level]).FromSaveData(boardObject);
                        break;
                }
            }
        }

        public GameData GetSaveDataForUtils()
        {
            _dataService ??= new FileDataService(new JsonSerializer());
            return _dataService.Load(SaveFileName);
        }

        public void DeleteSave()
        {
            _dataService.Delete(SaveFileName);
            ResetSave();
        }

        public void ResetSave()
        {
            _gameManager.ResetOnLoad();
            _activeSaveData.Clear();
            gameData = new GameData
            {
                currentPoints = 0,
                currentUpgradePoints = 0,
                currentObjective = string.Empty,
                boardObjects = new List<BoardObjectSaveData>(),
                unlockedCells = new List<Vector2Int>(),
                upgrades = new List<UpgradeSaveObject>()
            };
            
            PurchaseManager.ResetCurrency();
            ObjectiveManager.ResetObjectives();
            GridManager.ResetCells();
            UpgradeManager.ResetUpgrades();
            
            var freeCell = GridManager.GetClosestCell(Vector2Int.zero, true,true);
            
            if (freeCell == null) return;
            
            UnlockCell(freeCell);
            var newObj = Instantiate(_gameManager.defaultStartingObject);
            freeCell.SetChildObject(newObj);
            newObj.Init();
            
            FindAnyObjectByType<CameraZoomController>().OnGameplayStart();
        }

        public void AddObject(Vector2Int position, BoardObjectSaveData data)
        {
            if(!_activeSaveData.TryAdd(position, data))
            {
                _activeSaveData[position] = data;
            }
        
            OnSaveChanged(null);
        }
    
        public void RemoveObject(Vector2Int position)
        {
            if(_activeSaveData.ContainsKey(position))
            {
                _activeSaveData.Remove(position);
            }
        
            OnSaveChanged(null);
        }

        public void UnlockCell(GridCell g, bool playAnimation = false)
        {
           if(gameData.unlockedCells.Contains(g.gridPosition))
               Debug.LogError("Trying to unlock already unlocked gridcell");
           else
           {
               gameData.unlockedCells.Add(g.gridPosition);
               g.Unlock();
               SaveGame();
               SystemEventManager.Send(SystemEventManager.GameEvent.GridCellUnlocked, g);
           }
        }

        public bool GetCellLocked(Vector2Int gridPosition)
        {
            return gameData.unlockedCells.Contains(gridPosition);
        }

        public void SaveUpgrade(Upgrade upgrade)
        {
            var saveData = upgrade.ToSaveObject();
            var existingSaveData = gameData.upgrades.FirstOrDefault(s => s.upgradeName == saveData.upgradeName);
            if (existingSaveData != null)
            {
                existingSaveData.currentLevel = saveData.currentLevel;
            }
            
            OnSaveChanged(null);
        }

        private void OnSaveChanged(object o)
        {
            if (!_isLoaded) return;
            
            gameData.currentPoints = PurchaseManager.GetCurrentCurrency();
            gameData.currentUpgradePoints = PurchaseManager.GetCurrentUpgradePoints();
            gameData.boardObjects = _activeSaveData.Values.ToList();
            gameData.currentObjective = ObjectiveManager.CurrentObjective.id;
            gameData.upgrades = UpgradeManager.GetUpgradeSaveData();
            SaveGame();
        }

        private void OnDisable()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencyAdded, OnSaveChanged);
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencySpent, OnSaveChanged);
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.ObjectiveUpdated, OnSaveChanged);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}