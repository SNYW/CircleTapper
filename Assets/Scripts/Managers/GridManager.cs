using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GridManager
{
    private const float CellSize = 0.5f;
    private static InWorldGridManager InWorldGridManager => Object.FindFirstObjectByType<InWorldGridManager>();
    
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }

    private static readonly Dictionary<Direction, Vector2Int> DirectionOffsets = new()
    {
        { Direction.Up, new Vector2Int(0, -1) },
        { Direction.Down, new Vector2Int(0, 1) },
        { Direction.Left, new Vector2Int(-1, 0) },
        { Direction.Right, new Vector2Int(1, 0) },
        { Direction.UpLeft, new Vector2Int(-1, -1) },
        { Direction.UpRight, new Vector2Int(1, -1) },
        { Direction.DownLeft, new Vector2Int(-1, 1) },
        { Direction.DownRight, new Vector2Int(1, 1) }
    };

    public static Dictionary<Vector2Int, GridCell> Init(Vector2 startPos, Vector2Int dimensions, GridCell gridCell)
    {
        Dispose();
        var grid = new Dictionary<Vector2Int, GridCell>();
        var width = dimensions.x;
        var height = dimensions.y;
    
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = startPos + new Vector2(x * CellSize, -y * CellSize);
                var newCell = Object.Instantiate(gridCell, position, Quaternion.identity, InWorldGridManager.transform);
                newCell.gridPosition = new Vector2Int(x, y);
                grid[new Vector2Int(x, y)] = newCell;
            }
        }

        foreach (var cell in grid.Values)
        {
            CacheNeighbors(cell, grid);
        }

        return grid;
    }

    public static void CacheNeighbors(GridCell cell, Dictionary<Vector2Int, GridCell> grid)
    {
        cell.Neighbors = new Dictionary<Direction, GridCell>();
        
        foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
        {
            Vector2Int offset = DirectionOffsets[dir];
            int nx = cell.gridPosition.x + offset.x;
            int ny = cell.gridPosition.y + offset.y;

            if (grid.TryGetValue(new Vector2Int(nx, ny), out var neighbourCell))
                cell.Neighbors[dir] = neighbourCell;
        }
    }
    
    public static GridCell GetClosestCell(Vector2 worldPosition, bool includeOccupied = false, bool lockedOnly = false)
    {
        GridCell closestCell = null;
        float closestDistance = float.MaxValue;

        if (InWorldGridManager == null || InWorldGridManager.Grid == null) return null;

        foreach (var cell in InWorldGridManager.Grid.Values)
        {
            if(!includeOccupied && cell.heldObject != null) continue;
            if(lockedOnly && !cell.locked) continue;
            if(!lockedOnly && cell.locked) continue;
            
            float distance = Vector2.Distance(worldPosition, cell.transform.position);
            
            if (distance > closestDistance) continue;
            closestDistance = distance;
            closestCell = cell;
        }

        return closestCell;
    }
    
    public static GridCell GetGridCell(Vector2Int gridPosition, bool includeOccupied = false)
    {
        foreach (var cell in InWorldGridManager.Grid.Values)
        {
            if(!includeOccupied && cell.heldObject != null) continue;

            if (cell.gridPosition == gridPosition)
            {
                return cell;
            }
        }

        return null;
    }
    
    public static void ResetCells()
    {
        foreach (var kvp in InWorldGridManager.Grid)
        {
            kvp.Value.Lock();
        }
    }
    
    public static void Dispose()
    {
        var grid = InWorldGridManager.Grid;
        grid ??= new Dictionary<Vector2Int, GridCell>();
        while (InWorldGridManager.transform.childCount > 0)
        {
            foreach(Transform child in InWorldGridManager.transform)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
        grid.Clear();
    }

    public static List<BoardObject> GetAllBoardItems()
    {
        return InWorldGridManager.Grid.Where(kvp => kvp.Value.heldObject != null).Select(kvp => kvp.Value.heldObject).ToList();
    }

    public static int GetCellCount()
    {
        return InWorldGridManager?.Grid?.Count ?? 0;
    }
}