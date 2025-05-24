using System;
using System.Collections.Generic;
using Managers;
using Persistence;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int gridPosition;
    public Dictionary<GridManager.Direction, GridCell> Neighbors;
    public BoardObject heldObject = null;
    public SpriteRenderer debugSprite;
    public GameObject debugParent;
    public LineRenderer debugLine;
    private GridCellDebugger debugger;

    private void Awake()
    {
        debugLine.enabled = false;
        debugger = GetComponentInChildren<GridCellDebugger>();
    }

    private void Update()
    {
        debugParent.SetActive(GameManager.DEBUGMODE);
        debugSprite.color = (heldObject != null && GameManager.DEBUGMODE) ? Color.red : Color.clear;

        if (GameManager.DEBUGMODE && heldObject != null)
        {
            debugger.bo = heldObject;
            if (heldObject.transform.position == transform.position) return;
            
            debugLine.enabled = true;
            debugLine.SetPosition(0, transform.position);
            debugLine.SetPosition(1, heldObject.transform.position);
         
        }
        else
        {
            debugLine.enabled = false;
            debugger.bo = null;
        }
    }

    public void SetChildObject(BoardObject boardObject)
    {
        RemoveChildObject();
        heldObject = boardObject;
        heldObject.parentCell = this;
        heldObject.transform.position = transform.position;
        
        SaveManager.Instance.AddObject(gridPosition, heldObject.ToSaveData());
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardObjectMoved, boardObject);
    }

    public void RemoveChildObject()
    {
        if (heldObject == null) return;

        SaveManager.Instance.RemoveObject(gridPosition);
        heldObject.parentCell = null;
        heldObject = null;
    }
}