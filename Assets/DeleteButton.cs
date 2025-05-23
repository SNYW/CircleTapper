using System.Collections;
using DG.Tweening;
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
        if (obj is not BoardObject bo) return;

        Vector2 objectScreenPos = Camera.main.WorldToScreenPoint(bo.transform.position);
        RectTransform buttonRect = GetComponent<RectTransform>();
        Vector2 buttonScreenPos = RectTransformUtility.WorldToScreenPoint(null, buttonRect.position);
        float dist = Vector2.Distance(objectScreenPos, buttonScreenPos);

        if (!(dist < 100f)) return;
        
        StartCoroutine(DeleteItem(bo));

        _cg.DOFade(0, 0.3f);
    }

    private IEnumerator DeleteItem(BoardObject bo)
    {
        yield return new WaitForEndOfFrame();
        
        var pos = bo.parentCell?.gridPosition;
        Destroy(bo.gameObject);
        if (pos.HasValue)
        {
            SaveManager.Instance.RemoveObject(pos.Value);
        }
        FMODUnity.RuntimeManager.PlayOneShotAttached(deleteObjectSFX, gameObject);
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