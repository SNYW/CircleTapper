using System;
using System.Collections.Generic;
using System.Linq;
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
            gameData = _dataService.Load(SaveFileName);

            _activeSaveData = new Dictionary<Vector2Int, BoardObjectSaveData>();
            PurchaseManager.OnGameLoad(gameData);
            
            if (gameData.currentPoints == 0)
            {
                OnSaveChanged(null);
                Debug.Log("No Save Data Found, Resetting");
                return;
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
                }
            }
        }

        public void DeleteSave()
        {
            _dataService.Delete(SaveFileName);
            _gameManager.ResetOnLoad();
            _activeSaveData.Clear();
            PurchaseManager.ResetCurrency();
            SaveGame();
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

        private void OnSaveChanged(object o)
        {
            if (!_isLoaded) return;
            
            gameData.currentPoints = PurchaseManager.GetCurrentCurrency();
            gameData.boardObjects = _activeSaveData.Values.ToList();
            SaveGame();
        }

        private void OnDisable()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencyAdded, OnSaveChanged);
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencySpent, OnSaveChanged);
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