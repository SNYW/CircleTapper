using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GridManager
{
    private const float CellSize = 0.5f;

    private static InWorldGridManager _inWorldGridManager;

    /// <summary>
    /// The scene's grid, resolved once rather than searched for on every access. This used to be
    /// an expression-bodied FindFirstObjectByType, so a single GetClosestCell cost three
    /// scene-wide searches — and that runs per buy button, per frame.
    /// <para>
    /// Unity's == null is true for a destroyed object, so a scene change re-resolves by itself.
    /// </para>
    /// </summary>
    private static InWorldGridManager InWorldGridManager
    {
        get
        {
            if (_inWorldGridManager == null) _inWorldGridManager = Object.FindFirstObjectByType<InWorldGridManager>();

            return _inWorldGridManager;
        }
    }

    /// <summary>Statics survive play sessions when domain reload is disabled.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnEnterPlayMode() => _inWorldGridManager = null;
    
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

    /// <summary>
    /// The direction pointing back the other way. A hex's tapTargets are the directions it looks
    /// in, so a circle finding a hex in direction d is only watched by it if that hex looks in
    /// the opposite direction.
    /// </summary>
    public static Direction Opposite(Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        Direction.UpLeft => Direction.DownRight,
        Direction.UpRight => Direction.DownLeft,
        Direction.DownLeft => Direction.UpRight,
        Direction.DownRight => Direction.UpLeft,
        _ => direction
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

        var grid = InWorldGridManager;
        if (grid == null || grid.Grid == null) return null;

        foreach (var cell in grid.Grid.Values)
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
    
    /// <summary>Height of the grid in cells. Beams clamp their range to this.</summary>
    public static int Height => InWorldGridManager != null ? InWorldGridManager.dimensions.y : 0;

    /// <summary>
    /// Direct lookup by grid position. The grid is already keyed by exactly this, so unlike
    /// <see cref="GetGridCell"/> it does not walk every cell to find one.
    /// </summary>
    public static GridCell GetCellAt(Vector2Int gridPosition)
    {
        var grid = InWorldGridManager;
        if (grid?.Grid == null) return null;

        return grid.Grid.TryGetValue(gridPosition, out GridCell cell) ? cell : null;
    }

    /// <summary>
    /// Fills <paramref name="into"/> with the cells directly above <paramref name="origin"/>,
    /// nearest first, stopping at the top of the grid. Takes the list to fill so a beam firing
    /// repeatedly does not allocate.
    /// <para>Screen-up is negative Y — see <see cref="DirectionOffsets"/>.</para>
    /// </summary>
    public static void CollectColumnAbove(Vector2Int origin, int range, List<GridCell> into)
    {
        into.Clear();

        for (int step = 1; step <= range; step++)
        {
            GridCell cell = GetCellAt(new Vector2Int(origin.x, origin.y - step));
            if (cell == null) break;

            into.Add(cell);
        }
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

    /// <summary>Income per second implied by what is currently on the board.</summary>
    public static int GetPassiveIncomeAmount()
    {
        return GetAllBoardItems().Sum(bo => bo.chainLevel + 1);
    }

    public static int GetCellCount()
    {
        return InWorldGridManager?.Grid?.Count ?? 0;
    }
}