using System.Collections;
using DG.Tweening;
using Managers;
using Persistence;
using UnityEngine;

public class DeleteButton : MonoBehaviour
{
    private CanvasGroup _cg;
    public FMODUnity.EventReference deleteObjectSFX;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _cg.alpha = 0;
        
        SystemEventManager.Subscribe(SystemEventManager.GameEvent.ObjectDragged, OnObjectDragged);
        SystemEventManager.Subscribe(SystemEventManager.GameEvent.ObjectDropped, OnObjectDropped);
    }

    private void OnObjectDropped(object obj)
    {
        _cg.DOFade(0, 0.3f);
        if (obj is not BoardObject bo) return;

        Vector2 objectScreenPos = Camera.main.WorldToScreenPoint(bo.transform.position);
        RectTransform buttonRect = GetComponent<RectTransform>();
        Vector2 buttonScreenPos = RectTransformUtility.WorldToScreenPoint(null, buttonRect.position);
        float dist = Vector2.Distance(objectScreenPos, buttonScreenPos);

        if (!(dist < 100f)) return;
        
        DeleteItem(bo);
    }

    private void DeleteItem(BoardObject bo)
    {
        var pos = bo.parentCell?.gridPosition;
        var effectPos = bo.transform.position;
        
        if (pos.HasValue)
        {
            GridManager.GetGridCell(pos.Value, true).RemoveChildObject();
        }
        Destroy(bo.gameObject);
        FMODUnity.RuntimeManager.PlayOneShotAttached(deleteObjectSFX, gameObject);
        EffectsManager.Instance.SpawnEffect(EffectsManager.EffectType.Deletion, effectPos);
    }
    
    private void OnObjectDragged(object obj)
    {
        if (obj is not BoardObject bo) return;
        _cg.DOFade(1, 0.3f);
    }

    private void OnDisable()
    {
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.ObjectDragged, OnObjectDragged);
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.ObjectDropped, OnObjectDropped);
    }
}