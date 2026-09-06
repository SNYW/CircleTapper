using System.Collections.Generic;
using Managers;
using UnityEngine;

[ExecuteInEditMode]
public class InWorldGridManager : MonoBehaviour
{
    public Vector2Int dimensions;
    public GridCell gridCell;
    public Dictionary<Vector2Int, GridCell> Grid;

    private void Awake()
    {
        Grid = new Dictionary<Vector2Int, GridCell>();
        
        foreach (var cell in GetComponentsInChildren<GridCell>(true))
        {
            Grid.Add(cell.gridPosition, cell);
        }

        foreach (var cell in Grid.Values)
        {
            cell.gameObject.SetActive(true);
            GridManager.CacheNeighbors(cell, Grid);
        }
    }

    private bool _debugVisible;

    /// <summary>
    /// The single driver for every cell's debug overlay. Off, this costs one bool comparison; on,
    /// it walks the grid — which is fine, because it only happens while debugging.
    /// <para>
    /// Cells and their debuggers used to run an Update each: ~252 callbacks a frame, doing work
    /// even with debug off.
    /// </para>
    /// </summary>
    private void Update()
    {
        bool debugActive = GameManager.DEBUGMODE;

        // Nothing to do, and nothing left over from a previous frame to clear.
        if (!debugActive && !_debugVisible) return;

        _debugVisible = debugActive;
        if (Grid == null) return;

        foreach (GridCell cell in Grid.Values) cell.RefreshDebugVisuals(debugActive);
    }

    public void InitGrid()
    {
        Grid = GridManager.Init(transform.position, dimensions, gridCell);
    }
}