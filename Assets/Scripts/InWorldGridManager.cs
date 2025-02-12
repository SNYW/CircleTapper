using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class InWorldGridManager : MonoBehaviour
{
    public Vector2Int dimensions;
    public GridCell gridCell;
    public Dictionary<Vector2Int, GridCell> grid;
    public void Init()
    {
        grid = GridManager.Init(Vector2.zero, dimensions, gridCell);
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
            gridManager.Init();
        }
        if (GUILayout.Button("Clear Grid"))
        {
            GridManager.Dispose();
        }
    }
}