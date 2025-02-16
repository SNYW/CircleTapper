using System.Collections.Generic;
using System.IO;
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
            _dataService = new BinaryDataService(new BinarySerializer());
            LoadGame(true);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencyAdded, OnSaveChanged);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencySpent, OnSaveChanged);
            
        }
        
        public void SaveGame() => _dataService.Save(SaveFileName, gameData);

        public void LoadGame(bool spawnObjects = false)
        {
            gameData = new GameData();
            gameData = _dataService.Load(SaveFileName);

            _activeSaveData = new Dictionary<Vector2Int, BoardObjectSaveData>();

            if (gameData.currentPoints == 0)
            {
                OnSaveChanged(null);
                
                GridManager.GetClosestCell(Vector2.zero).SetChildObject(Instantiate(_gameManager.defaultStartingObject));
                Debug.Log("No Save Data Found, Resetting");
                return;
            }
            
            foreach (var boardObject in gameData.boardObjects)
            {
                _activeSaveData.Add(new Vector2Int(boardObject.xPosition, boardObject.yPosition), boardObject);

                if (!spawnObjects) continue;

                var boardPos = new Vector2Int(boardObject.xPosition, boardObject.yPosition);
                switch (boardObject)
                {
                    case CircleSaveData circleSaveData:
                        GridManager.GetGridCell(boardPos).SetChildObject(Instantiate(_gameManager.circleLevels[circleSaveData.level]));
                        break;
                    case SquareSaveData squareSaveData:
                        GridManager.GetGridCell(boardPos).SetChildObject(Instantiate(_gameManager.circleLevels[squareSaveData.level]));
                        break;
                }
            }
            
            PurchaseManager.OnGameLoad(gameData);
        }

        public void DeleteSave() => _dataService.Delete(SaveFileName);

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
            gameData.currentPoints = PurchaseManager.GetCurrentCurrency();
            gameData.boardObjects = _activeSaveData.Values.ToList();
            SaveGame();
        }

        private void OnDisable()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencyAdded, OnSaveChanged);
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencySpent, OnSaveChanged);
        }
    }

    [CustomEditor(typeof(SaveManager))]
    public class SaveManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SaveManager saveManager = (SaveManager)target;
            DrawDefaultInspector();

            if (GUILayout.Button("Save"))
            {
                saveManager.SaveGame();
            }

            if (GUILayout.Button("Load"))
            {
                saveManager.LoadGame(true);
            }

            if (GUILayout.Button("Delete"))
            {
                saveManager.DeleteSave();
            }
        }
    }
}