using System;
using System.Collections;
using UnityEngine;

public class Square : BoardObject
{
    public float clickSpeed;

    private void Awake()
    {
        GridManager.GetClosestCell(transform.position).SetChildObject(this);
    }

    private void Start()
    {
        StartCoroutine(ClickNeighbours());
    }

    private IEnumerator ClickNeighbours()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(clickSpeed);

            if(ParentCell == null) continue;
            
            foreach (var neighbor in ParentCell.Neighbors)
            {
                if (neighbor.Value?.heldObject is Circle circle)
                {
                    circle.OnMouseDown();
                }
            }
        }
    }
}
