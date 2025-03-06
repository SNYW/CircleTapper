using System;
using System.Collections.Generic;
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

    private void Awake()
    {
        debugLine.enabled = false;
    }

    private void Update()
    {
        debugParent.SetActive(GameManager.DEBUGMODE);
        debugSprite.color = (heldObject != null && GameManager.DEBUGMODE) ? Color.red : Color.clear;

        if (GameManager.DEBUGMODE && heldObject != null)
        {
            if (heldObject.transform.position == transform.position) return;
            
            debugLine.enabled = true;
            debugLine.SetPosition(0, transform.position);
            debugLine.SetPosition(1, heldObject.transform.position);
        }
        else
        {
            debugLine.enabled = false;
        }
    }

    public void SetChildObject(BoardObject boardObject)
    {
        RemoveChildObject();
        heldObject = boardObject;
        heldObject.parentCell = this;
        heldObject.transform.position = transform.position;
        
        SaveManager.Instance.AddObject(gridPosition, heldObject.ToSaveData());
    }

    public void RemoveChildObject()
    {
        if (heldObject == null) return;

        SaveManager.Instance.RemoveObject(gridPosition);
        heldObject.parentCell = null;
        heldObject = null;
    }
}