using Core;
using System;
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

    public FMODUnity.EventReference MergeObjectSFX;

    private void OnEnable()
    {
        SetIndicators(false);
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }

    public virtual void Init() { }

    public virtual void BeginDrag(Vector2 touchPosition)
    {
        parentCell?.RemoveChildObject();
    }

    public virtual void OnDrag(Vector2 worldPosition)
    {
        SetIndicators(true);
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
        SetIndicators(false);
    }

    public virtual void OnTap() { }

    public virtual void OnMerge(BoardObject targetObj)
    {
        var newItem = Instantiate(onMergeSpawn, targetObj.transform.position, quaternion.identity);
        ServiceLocator.Get<SaveService>().RemoveBoardObject(targetObj.parentCell.gridPosition);
        targetObj.parentCell.SetChildObject(newItem);
        newItem.Init();
        Destroy(targetObj.gameObject);
        Destroy(gameObject);
        FMODUnity.RuntimeManager.PlayOneShotAttached(MergeObjectSFX, gameObject);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        // Tweens are linked to their GameObject, so DOTween kills them on destroy by itself.
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }

    private void SetIndicators(bool active)
    {
        if (influenceIndicators is not { Count: > 0 }) return;
        foreach (var indicator in influenceIndicators)
            indicator.gameObject.SetActive(active);
    }

    public abstract BoardObjectSaveData ToSaveData();

    public abstract void FromSaveData(BoardObjectSaveData saveData);

    /// <summary>
    /// Writes this object's state into the save. A no-op while the object is unparented — mid-drag
    /// it belongs to no cell, so there is no position to key it by.
    /// </summary>
    protected void SaveObjectState()
    {
        if (parentCell == null) return;

        ServiceLocator.Get<SaveService>().SetBoardObject(parentCell.gridPosition, ToSaveData());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public abstract string GetValue(); 
    public abstract string GetMaterialValue();
}