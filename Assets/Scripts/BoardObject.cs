using System.Collections.Generic;
using DG.Tweening;
using Persistence;
using Unity.Mathematics;
using UnityEngine;

public abstract class BoardObject : MonoBehaviour, ISaveable
{
    public int chainLevel;
    public GridCell parentCell;
    public BoardObject onMergeSpawn;
    public List<GameObject> influenceIndicators;

    private bool _isDragging = false;

    private void OnEnable()
    {
        SetIndicators(false);
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }

    public virtual void Init()
    {
        
    }

    public virtual void BeginDrag(Vector2 touchPosition)
    {
        if (_isDragging) return;

        parentCell.RemoveChildObject();
        _isDragging = true;
        
        SetIndicators(true);
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
                if (onMergeSpawn != null && cell.heldObject.gameObject.name == gameObject.name)
                {
                    OnMerge(cell.heldObject);
                    return;
                }
            }
            cell = GridManager.GetClosestCell(touchPosition);
        }
        
        cell.SetChildObject(this);
        _isDragging = false;
        
        SetIndicators(false);
    }

    public virtual void OnTap()
    {
    }

    public virtual void OnMerge(BoardObject targetObj)
    {
        var newItem = Instantiate(onMergeSpawn, targetObj.transform.position, quaternion.identity);
        SaveManager.Instance.RemoveObject(targetObj.parentCell.gridPosition);
        targetObj.parentCell.SetChildObject(newItem);
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

    private void SetIndicators(bool active)
    {
        if (influenceIndicators is not { Count: > 0 }) return;
        
        foreach (var indicator in influenceIndicators)
        {
            indicator.gameObject.SetActive(active);
        }
    }

    public virtual BoardObjectSaveData ToSaveData()
    {
        throw new System.NotImplementedException();
    }

    public virtual void FromSaveData(BoardObjectSaveData saveData)
    {
        throw new System.NotImplementedException();
    }

    protected virtual void SaveObjectState()
    {
        throw new System.NotImplementedException();
    }
}