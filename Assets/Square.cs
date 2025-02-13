using System.Collections;
using DG.Tweening;
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

    public override void BeginDrag(Vector2 startPos)
    {
        StopAllCoroutines();
        base.BeginDrag(startPos);
    }

    public override void EndDrag(Vector2 eventData)
    {
        StopAllCoroutines();
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
            transform.DOScale(1.2f, 0.1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.DOScale(1f, 0.1f)
                    .SetEase(Ease.InQuad));
        }
    }
}
