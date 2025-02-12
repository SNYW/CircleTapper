using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int gridPosition;
    public Dictionary<GridManager.Direction, GridCell> Neighbors = new();

    public BoardObject heldObject = null;

    public GridCell(Vector2Int position)
    {
        gridPosition = position;
    }
    
    public void SetChildObject(BoardObject boardObject)
    {
        heldObject = boardObject;
        heldObject.ParentCell = this;
        heldObject.transform.position = transform.position;
    }
}