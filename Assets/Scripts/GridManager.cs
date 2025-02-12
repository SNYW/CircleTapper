using System.Collections.Generic;
using UnityEngine;

public static class GridManager
{
    private const float CellSize = 1.2f;
    private static InWorldGridManager _inWorldGridManager => Object.FindFirstObjectByType<InWorldGridManager>();
    
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
        Vector2 origin = startPos - new Vector2((width / 2f) * CellSize, (height / 2f) * CellSize);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 position = origin + new Vector2(x * CellSize, y * CellSize);
                var newCell = Object.Instantiate(gridCell, position, Quaternion.identity, _inWorldGridManager.transform);
                grid[new Vector2Int(x, y)] = newCell;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CacheNeighbors(new Vector2Int(x,y), dimensions, grid);
            }
        }

        return grid;
    }

    private static void CacheNeighbors(Vector2Int pos, Vector2Int dimensions, Dictionary<Vector2Int, GridCell> grid)
    {
        GridCell cell = grid[pos];
        cell.Neighbors = new Dictionary<Direction, GridCell>();
        
        foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
        {
            Vector2Int offset = DirectionOffsets[dir];
            int nx = pos.x + offset.x;
            int ny = pos.y + offset.y;

            if (nx >= 0 && nx < dimensions.x && ny >= 0 && ny < dimensions.y)
            {
                cell.Neighbors[dir] = grid[new Vector2Int(nx,ny)];
            }
        }
    }
    
    public static GridCell GetClosestCell(Vector2 worldPosition, bool includeOccupied = false)
    {
        GridCell closestCell = null;
        float closestDistance = float.MaxValue;
        float minSnapDistance = 10f;

        foreach (var cell in _inWorldGridManager.grid.Values)
        {
            if(!includeOccupied && cell.heldObject != null) continue;
            
            float distance = Vector2.Distance(worldPosition, cell.transform.position);
            
            if (distance > closestDistance) continue;
            if(distance > minSnapDistance) continue;
            
            closestDistance = distance;
            closestCell = cell;
        }

        return closestCell;
    }


    public static void Dispose()
    {
        var grid = _inWorldGridManager.grid;
        grid ??= new Dictionary<Vector2Int, GridCell>();
        foreach (var gridValue in grid.Values)
        {
            if (gridValue != null)
                Object.DestroyImmediate(gridValue.gameObject);
        }
        grid.Clear();
    }
}