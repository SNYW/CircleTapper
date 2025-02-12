using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class InWorldGridManager : MonoBehaviour
{
    public Vector2Int dimensions;
    public GridCell gridCell;
    public Dictionary<Vector2Int, GridCell> Grid;

    private void Awake()
    {
        Grid = new Dictionary<Vector2Int, GridCell>();
        
        foreach (var cell in GetComponentsInChildren<GridCell>())
        {
            Grid.Add(cell.gridPosition, cell);
        }

        foreach (var cell in Grid.Values)
        {
            GridManager.CacheNeighbors(cell, Grid);
        }
    }

    public void InitGrid()
    {
        Grid = GridManager.Init(transform.position, dimensions, gridCell);
    }
}

[CustomEditor(typeof(InWorldGridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InWorldGridManager gridManager = (InWorldGridManager)target;
        if (GUILayout.Button("Generate Grid"))
        {
            gridManager.InitGrid();
        }
        if (GUILayout.Button("Clear Grid"))
        {
            GridManager.Dispose();
        }
    }
}