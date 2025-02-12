using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int gridPosition;
    public Dictionary<GridManager.Direction, GridCell> Neighbors;
    public BoardObject heldObject = null;
    private SpriteRenderer spriteRenderer;

    private Color startColor;
    public GridCell(Vector2Int position)
    {
        gridPosition = position;
    }

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        startColor = spriteRenderer.color;
    }

    /*private void Update()
    {
        spriteRenderer.color = heldObject == null ? startColor : Color.red;
    }*/

    public void SetChildObject(BoardObject boardObject)
    {
        heldObject = boardObject;
        heldObject.ParentCell = this;
        heldObject.transform.position = transform.position;
    }
}