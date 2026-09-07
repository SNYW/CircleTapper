using Progression;
using Core;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private BoardObject _draggedObject;
    private bool _isDragging;
    private Vector2 _startTouchPosition;
    private float _startTouchTime;

    private const float DragThreshold = 0.2f;
    private const float TapTimeThreshold = 0.2f;

    private void Update()
    {
        if (Input.touchCount <= 0) return;

        Touch touch = Input.touches[0];
        if (Camera.main == null) return;
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(touch.position);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _startTouchPosition = worldPosition;
                _startTouchTime = Time.time;
                TrySetDraggedObject(worldPosition);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                HandleTouchMoved(worldPosition);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                HandleTouchEnded(worldPosition);
                break;
        }
    }

    private void TrySetDraggedObject(Vector2 worldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
        if (hit.collider == null) return;
        if (!hit.collider.TryGetComponent(out _draggedObject)) return;
    }

    private void HandleTouchMoved(Vector2 worldPosition)
    {
        if (_draggedObject == null) return;

        float distance = Vector2.Distance(worldPosition, _startTouchPosition);
        if (!_isDragging && distance >= DragThreshold)
        {
            _draggedObject.BeginDrag(_startTouchPosition);
            _isDragging = true;
            SystemEventManager.Send(SystemEventManager.GameEvent.ObjectDragged, _draggedObject);
        }

        if (!_isDragging) return;
        
        _draggedObject.OnDrag(worldPosition);
    }

    private void HandleTouchEnded(Vector2 worldPosition)
    {
        float duration = Time.time - _startTouchTime;
        float distance = Vector2.Distance(worldPosition, _startTouchPosition);

        if (_draggedObject != null)
        {
            if (_isDragging)
            {
                if (!FindAnyObjectByType<DeleteButton>().TryDeleteObject(_draggedObject))
                    _draggedObject.EndDrag(worldPosition);
            }
            else if (distance < DragThreshold && duration < TapTimeThreshold)
            {
                _draggedObject.OnTap();

                // Deliberately here and not in Circle.OnTap: squares and triangle beams call
                // that too, and they must not complete a "tap a circle" objective for the player.
                if (_draggedObject is Circle && ServiceLocator.TryGet(out ObjectiveService objectives))
                {
                    objectives.Report(ObjectiveGoal.Tap);
                }
            }
        }

        SystemEventManager.Send(SystemEventManager.GameEvent.ObjectDropped, _draggedObject);
        _draggedObject = null;
        _isDragging = false;
    }
}