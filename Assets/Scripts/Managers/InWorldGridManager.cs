using System.Collections.Generic;
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

    private void Start()
    {
        SystemEventManager.Subscribe(SystemEventManager.GameEvent.BoardObjectMoved, OnBoardObjectMoved);
    }

    private void OnBoardObjectMoved(object obj)
    {
        
    }

    public void InitGrid()
    {
        Grid = GridManager.Init(transform.position, dimensions, gridCell);
    }

    private void OnDisable()
    {
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.BoardObjectMoved, OnBoardObjectMoved);
    }
}