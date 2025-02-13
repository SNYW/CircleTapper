using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    private BoardObject _draggedObject;

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];
            Vector2 touchPosition = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touchPosition);
                    break;

                case TouchPhase.Moved:
                    HandleTouchMoved(touchPosition);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnded(touchPosition);
                    break;
            }
        }
    }

    private void HandleTouchBegan(Vector2 touchPosition)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, 0));
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider == null) return;
        if (!hit.collider.TryGetComponent<BoardObject>(out var boardObject)) return;
        
        boardObject.OnTap();
        _draggedObject = boardObject;
    }

    private void HandleTouchMoved(Vector2 touchPosition)
    {
        if (_draggedObject == null) return;

        _draggedObject.BeginDrag();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, 0));
        var campos = GridManager.GetClosestCell(worldPosition,true);
        _draggedObject.transform.position = campos.transform.position;
    }

    private void HandleTouchEnded(Vector2 touchPosition)
    {
        if (_draggedObject != null)
        {
            _draggedObject.EndDrag(touchPosition);
        }
        _draggedObject = null; 
    }
}