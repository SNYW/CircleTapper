using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BoardObject : MonoBehaviour
{
    public GridCell ParentCell;
    public GridCell LastParentCell;

    private bool _isDragging = false;
    
    public virtual void BeginDrag()
    {
        if (_isDragging) return;
        
        LastParentCell = ParentCell;
        ParentCell.heldObject = null;
        ParentCell = null;
        _isDragging = true;
    }

    public virtual void EndDrag(Vector2 eventData)
    {
        var cell = GridManager.GetClosestCell(Camera.main.ScreenToWorldPoint(eventData), true);

        if (cell.heldObject != null && LastParentCell != null)
        {
            LastParentCell.SetChildObject(cell.heldObject);
            cell.heldObject = null;
        }
        
        cell.SetChildObject(this);
        LastParentCell = null;
        _isDragging = false;
    }

    public virtual void OnTap()
    {
        
    }
}