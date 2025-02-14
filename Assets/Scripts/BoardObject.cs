using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

public abstract class BoardObject : MonoBehaviour
{
    public GridCell ParentCell;
    public GridCell LastParentCell;
    public BoardObject onMergeSpawn;

    private bool _isDragging = false;
    private Vector2 _startPosition;

    private void OnEnable()
    {
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }

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
            if (cell.heldObject.GetType() == GetType())
            {
                OnMerge(cell.heldObject);
            }
            cell = GridManager.GetClosestCell(touchPosition);
        }
        
        cell.SetChildObject(this);
        LastParentCell = null;
        _isDragging = false;
    }

    public virtual void OnTap()
    {
    }

    public virtual void OnMerge(BoardObject targetObj)
    {
        if (onMergeSpawn == null) return;
        if (targetObj.gameObject.name != gameObject.name) return;
        
        var newItem = Instantiate(onMergeSpawn, transform.position, quaternion.identity);
        GridManager.GetClosestCell(transform.position, true).SetChildObject(newItem);
        Destroy(targetObj.gameObject);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        DOTween.KillAll(gameObject);
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }
}