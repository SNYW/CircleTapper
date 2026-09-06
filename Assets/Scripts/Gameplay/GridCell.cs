using Core;
using System.Collections.Generic;
using DG.Tweening;
using Managers;
using Persistence;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public SpriteRenderer innerSprite;
    public Vector2Int gridPosition;
    public Dictionary<GridManager.Direction, GridCell> Neighbors;
    public BoardObject heldObject = null;
    public SpriteRenderer debugSprite;
    public GameObject debugParent;
    public LineRenderer debugLine;
    private GridCellDebugger debugger;
    public bool locked = true;
    private void Awake()
    {
        Lock();
        debugLine.enabled = false;
        debugger = GetComponentInChildren<GridCellDebugger>(true);
    }

    /// <summary>
    /// Driven by <see cref="InWorldGridManager"/> rather than an Update of its own. Every cell
    /// having one meant ~126 callbacks a frame doing nothing but keeping hidden things hidden.
    /// </summary>
    public void RefreshDebugVisuals(bool debugActive)
    {
        if (debugParent.activeSelf != debugActive) debugParent.SetActive(debugActive);

        bool showsObject = debugActive && heldObject != null;
        debugSprite.color = showsObject ? Color.red : Color.clear;
        debugger.bo = showsObject ? heldObject : null;

        // Only worth a line when the object has drifted off its cell, i.e. while it is dragged.
        bool showsLine = showsObject && heldObject.transform.position != transform.position;
        if (debugLine.enabled != showsLine) debugLine.enabled = showsLine;

        if (showsLine)
        {
            debugLine.SetPosition(0, transform.position);
            debugLine.SetPosition(1, heldObject.transform.position);
        }

        debugger.Refresh();
    }

    public void SetChildObject(BoardObject boardObject)
    {
        RemoveChildObject();
        heldObject = boardObject;
        heldObject.parentCell = this;
        heldObject.transform.position = transform.position;

        ServiceLocator.Get<SaveService>().SetBoardObject(gridPosition, heldObject.ToSaveData());
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardObjectMoved, boardObject);
    }

    public void Lock()
    {
        locked = true;
        innerSprite.color = Color.clear;
    }

    public void Unlock(bool playAnimation = true)
    {
        locked = false;
        innerSprite.gameObject.SetActive(true);
        innerSprite.color = Color.white;
        innerSprite.DOKill();
        innerSprite.DOFade(0.2f, 0.3f).SetLink(gameObject);

        if (!playAnimation) return;

        innerSprite.transform.DOKill();
        innerSprite.transform
            .DOPunchScale(innerSprite.transform.localScale * 1.2f, 0.2f)
            .SetLink(gameObject);
    }

    public void RemoveChildObject()
    {
        if (heldObject == null) return;

        ServiceLocator.Get<SaveService>().RemoveBoardObject(gridPosition);
        heldObject.parentCell = null;
        heldObject = null;
    }
}