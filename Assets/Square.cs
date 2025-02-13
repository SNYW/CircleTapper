using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Square : BoardObject
{
    public float clickSpeed;
    public ParticleSystem clickParticles;

    private void Start()
    {
        if(ParentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);
        StartCoroutine(ClickNeighbours());
    }

    public override void BeginDrag()
    {
        StopAllCoroutines();
        base.BeginDrag();
    }

    public override void EndDrag(Vector2 eventData)
    {
        StartCoroutine(ClickNeighbours());
        base.EndDrag(eventData);
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
                    circle.OnTap();
                }
            }
            clickParticles.Play();
        }
    }
}
