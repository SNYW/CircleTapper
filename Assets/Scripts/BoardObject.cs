using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BoardObject : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public GridCell ParentCell;
    public GridCell LastParentCell;
    
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        LastParentCell = ParentCell;
        ParentCell.heldObject = null;
        ParentCell = null;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        var cell = GridManager.GetClosestCell(Camera.main.ScreenToWorldPoint(eventData.position), true);

        if (cell.heldObject != null && LastParentCell != null)
        {
            LastParentCell.SetChildObject(cell.heldObject);
            cell.heldObject = null;
        }
        
        cell.SetChildObject(this);
        LastParentCell = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var campos = GridManager.GetClosestCell(Camera.main.ScreenToWorldPoint(eventData.position),true);
        transform.position = campos.transform.position;
    }
}