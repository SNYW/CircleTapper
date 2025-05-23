using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    private BoardObject _draggedObject;

    private void Update()
    {
        if (Input.touchCount <= 0) return;
        
        Touch touch = Input.touches[0];
        Vector2 touchPosition = touch.position;
        if (Camera.main == null) return;
        
        var worldPosition = Camera.main.ScreenToWorldPoint(touchPosition);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                HandleTouchBegan(worldPosition);
                break;

            case TouchPhase.Moved:
                HandleTouchMoved(worldPosition);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                HandleTouchEnded(worldPosition);
                break;
        }
    }

    private void HandleTouchBegan(Vector2 worldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider == null) return;
        if (!hit.collider.TryGetComponent<BoardObject>(out var boardObject)) return;
        
        boardObject.OnTap();
        _draggedObject = boardObject;
        SystemEventManager.Send(SystemEventManager.GameEvent.ObjectDragged, _draggedObject);
    }

    private void HandleTouchMoved(Vector2 worldPosition)
    {
        if (_draggedObject == null) return;
        
        _draggedObject.BeginDrag(worldPosition);
        _draggedObject.OnDrag(worldPosition);
    }

    private void HandleTouchEnded(Vector2 worldPosition)
    {
        if (_draggedObject != null)
        {
            SystemEventManager.Send(SystemEventManager.GameEvent.ObjectDropped, _draggedObject);
            _draggedObject.EndDrag(worldPosition);
        }
        _draggedObject = null; 
    }
}