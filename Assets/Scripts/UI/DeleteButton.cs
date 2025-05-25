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
    
    public bool CanDelete(BoardObject bo)
    {
        Vector2 objectScreenPos = Camera.main.WorldToScreenPoint(bo.transform.position);
        RectTransform buttonRect = GetComponent<RectTransform>();
        Vector2 buttonScreenPos = RectTransformUtility.WorldToScreenPoint(null, buttonRect.position);
        float dist = Vector2.Distance(objectScreenPos, buttonScreenPos);
        return dist < 100f;
    }
    
    private void OnObjectDragged(object obj)
    {
        _cg.DOFade(1, 0.3f);
    }

    private void OnDisable()
    {
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.ObjectDragged, OnObjectDragged);
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.ObjectDropped, OnObjectDropped);
    }

    private void OnObjectDropped(object obj)
    {
        _cg.DOFade(0, 0.3f);
    }

    public bool TryDeleteObject(BoardObject boardObject)
    {
        var canDelete = CanDelete(boardObject);

        if (!canDelete) return canDelete;
        
        var effectPos = boardObject.transform.position;
        Destroy(boardObject.gameObject);
        FMODUnity.RuntimeManager.PlayOneShotAttached(deleteObjectSFX, gameObject);
        EffectsManager.Instance.SpawnEffect(EffectsManager.EffectType.Deletion, effectPos);
        
        return canDelete;
    }
}