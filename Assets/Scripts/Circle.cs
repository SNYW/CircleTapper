using System;
using TMPro;
using UnityEngine;
using static SystemEventManager.GameEvent;

public class Circle : BoardObject
{
    public TMP_Text numberDisplay;
    public int startValue;
    public int currentValue;

    private int _currentStartValue;
    
    private void Start()
    {
        currentValue = startValue;
        _currentStartValue = startValue;
        if(ParentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);
    }

    public void OnMouseDown()
    {
        currentValue--;

        if (currentValue <= 0)
        {
           Complete();
        }

        numberDisplay.SetText(currentValue.ToString());
    }

    private void Complete()
    {
        SystemEventManager.Send(CircleComplete, _currentStartValue);
        _currentStartValue *= 2;
        currentValue = _currentStartValue;
    }

    public Vector2 GetPosition()
    {
        return transform.position;
    }
}
