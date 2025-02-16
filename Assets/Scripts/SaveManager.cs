using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static Dictionary<Vector2Int, BoardObjectSaveData> _save;
    
    private const string SaveFileName = "CTSave.json";
    
    public static void Init(GameManager manager)
    {
        _save = new Dictionary<Vector2Int, BoardObjectSaveData>();
        
        string path = Application.persistentDataPath + "/" + SaveFileName;
        if (!File.Exists(path))
        {
           SaveBoard();
           return;
        }
        
        string json = File.ReadAllText(path);
        _save = JsonUtility.FromJson<Dictionary<Vector2Int, BoardObjectSaveData>>(json);

        foreach (var saveData in _save)
        {
            switch (saveData.Value)
            {
                case CircleSaveData circleSaveData:
                    LoadCircle(circleSaveData, manager);
                    break;
                case SquareSaveData squareSaveData:
                    LoadSquare(squareSaveData, manager);
                    break;
            }
        }
        
        Debug.Log($"Loaded Board with {_save.Count} Object(s)");
    }

    private static void LoadCircle(CircleSaveData data, GameManager gm)
    {
        var newObj = Object.Instantiate(gm.circleLevels[data.level]);
        newObj.FromSaveData(data);
        GridManager.GetClosestCell(data.position).SetChildObject(newObj);
    }
    
    private static void LoadSquare(SquareSaveData data, GameManager gm)
    {
        var newObj = Object.Instantiate(gm.squareLevels[data.level]);
        newObj.FromSaveData(data);
        GridManager.GetClosestCell(data.position).SetChildObject(newObj);
    }
    
    
    
    public static void SaveBoard()
    {
        string json = JsonUtility.ToJson(_save, true);
        File.WriteAllText(Application.persistentDataPath + "/" + SaveFileName, json);
        Debug.Log($"Saved Board with {_save.Count} Object(s)");
    }

    public static void SaveBoardObject(BoardObjectSaveData data)
    {
        if (!_save.TryAdd(data.position, data))
        {
            _save[data.position] = data;
        }
        
        SaveBoard();
    }

    public static void RemoveSaveData(Vector2Int key)
    {
        if (!_save.ContainsKey(key)) return;
        
        _save.Remove(key);
        SaveBoard();
    }
}