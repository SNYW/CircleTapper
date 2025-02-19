using DG.Tweening;
using Persistence;
using Unity.Mathematics;
using UnityEngine;

public abstract class BoardObject : MonoBehaviour, ISaveable
{
    public int chainLevel;
    public GridCell ParentCell;
    public BoardObject onMergeSpawn;

    private bool _isDragging = false;

    private void OnEnable()
    {
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }

    public virtual void Init()
    {
        
    }

    public virtual void BeginDrag(Vector2 touchPosition)
    {
        if (_isDragging) return;

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
                return;
            }
            cell = GridManager.GetClosestCell(touchPosition);
        }
        
        cell.SetChildObject(this);
        _isDragging = false;
    }

    public virtual void OnTap()
    {
    }

    public virtual void OnMerge(BoardObject targetObj)
    {
        if (onMergeSpawn == null) return;
        if (targetObj.gameObject.name != gameObject.name) return;
        
        var newItem = Instantiate(onMergeSpawn, targetObj.transform.position, quaternion.identity);
        SaveManager.Instance.RemoveObject(targetObj.ParentCell.gridPosition);
        targetObj.ParentCell.SetChildObject(newItem);
        newItem.Init();
        Destroy(targetObj.gameObject);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        DOTween.KillAll(gameObject);
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }

    public virtual BoardObjectSaveData ToSaveData()
    {
        throw new System.NotImplementedException();
    }

    public virtual void FromSaveData(BoardObjectSaveData saveData)
    {
        throw new System.NotImplementedException();
    }
}