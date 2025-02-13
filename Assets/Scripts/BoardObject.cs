using UnityEngine;

public abstract class BoardObject : MonoBehaviour
{
    public GridCell ParentCell;
    public GridCell LastParentCell;

    private bool _isDragging = false;
    private Vector2 _startPosition;

    public virtual void BeginDrag(Vector2 touchPosition)
    {
        if (_isDragging) return;

        _startPosition = touchPosition;
        if(ParentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);
        ParentCell.RemoveChildObject();
        _isDragging = true;
    }

    public virtual void OnDrag(Vector2 worldPosition)
    {
        transform.position = worldPosition;
    }

    public virtual void EndDrag(Vector2 touchPosition)
    {
        var cell = GridManager.GetClosestCell(touchPosition, true);

        if (cell.heldObject != null && cell.heldObject != this)
        {
            cell = GridManager.GetClosestCell(touchPosition);
        }
        
        cell.SetChildObject(this);
        LastParentCell = null;
        _isDragging = false;
    }

    public virtual void OnTap()
    {
    }
}