using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Persistence;
using System;
using Gameplay;

public class GridVisualizerWithData : EditorWindow
{
    private int gridWidth = 10;
    private int gridHeight = 10;
    private float cellSize = 40f; 
    private Dictionary<Vector2Int, BoardObjectSaveData> occupiedCells = new();
    private Vector2 upgradeScrollPos;
    private List<UpgradeSaveObject> upgrades = new();

    private void OnEnable()
    {
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }
    
    [MenuItem("Tools/Grid Visualizer With Save Data")]
    public static void OpenWindow()
    {
        GetWindow<GridVisualizerWithData>("Grid Visualizer");
    }

    private void OnGUI()
    {
        LoadSaveData();

        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
        gridWidth = EditorGUILayout.IntField("Grid Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Grid Height", gridHeight);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);

        Rect gridRect = GUILayoutUtility.GetRect(gridWidth * cellSize, gridHeight * cellSize, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawGrid(gridRect);

        GUILayout.Space(20);
        GUILayout.Label("Upgrades", EditorStyles.boldLabel);

        upgradeScrollPos = GUILayout.BeginScrollView(upgradeScrollPos, GUILayout.Height(150));
        if (upgrades != null)
        {
            foreach (var upgrade in upgrades)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label((string)upgrade.upgradeName, GUILayout.Width(200));
                GUILayout.Label("Level: " + upgrade.currentLevel);
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();
    }


    private void DrawGrid(Rect area)
    {
        Handles.BeginGUI();
        Color oldColor = Handles.color;
        Handles.color = Color.gray;

        for (int x = 0; x <= gridWidth; x++)
        {
            float px = area.x + x * cellSize;
            Handles.DrawLine(new Vector3(px, area.y), new Vector3(px, area.y + gridHeight * cellSize));
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            float py = area.y + y * cellSize;
            Handles.DrawLine(new Vector3(area.x, py), new Vector3(area.x + gridWidth * cellSize, py));
        }

        Handles.color = oldColor;
        Handles.EndGUI();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new(x, y);
                Rect cellRect = new Rect(area.x + x * cellSize, area.y + y * cellSize, cellSize, cellSize);

                if (!occupiedCells.TryGetValue(pos, out var data)) continue;
                
                GUI.color = new Color(0.3f, 0.8f, 0.3f, 0.3f);
                GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                var typeLabel = data.type;
                var levelLabel = $"Lv {data.level}";

                var typeRect = new Rect(cellRect.x, cellRect.y + 2, cellSize, 16);
                var levelRect = new Rect(cellRect.x, cellRect.y + 18, cellSize, 16);

                GUI.Label(typeRect, typeLabel, EditorStyles.centeredGreyMiniLabel);
                GUI.Label(levelRect, levelLabel, EditorStyles.centeredGreyMiniLabel);
            }
        }
    }

    private void LoadSaveData()
    {
        GameData gameData = FindAnyObjectByType<SaveManager>().GetSaveDataForUtils();

        occupiedCells.Clear();
        foreach (var obj in gameData.boardObjects)
        {
            Vector2Int pos = new(obj.xPosition, obj.yPosition);
            occupiedCells[pos] = obj;
        }

        upgrades = gameData.upgrades;
        Repaint();
    }
}