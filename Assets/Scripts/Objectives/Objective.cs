using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;

namespace Objectives
{
    public class Objective
    {
        public string id;
        public TargetType targetType;
        public int current;
        public int amount;
        public float Percentage => (float)current / amount;
        public bool Claimable => current >= amount;

        public Action<int> OnProgressed;
        public Action<int> OnCompleted;

        public enum TargetType
        {
            Points
        }

        public Objective(string id, string target, string amountString)
        {
            this.id = id;
            targetType = Enum.Parse<TargetType>(target);
            amount = int.Parse(amountString);
            current = GetTargetValue();
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencySpent, UpdateCurrentValue);
            SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencyAdded, UpdateCurrentValue);
        }

        private void UpdateCurrentValue(object obj)
        {
            current = GetTargetValue();
            OnProgressed?.Invoke(current);
        }

        private int GetTargetValue()
        {
            return targetType switch
            {
                TargetType.Points => (int)PurchaseManager.GetCurrentCurrency(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        public string GetDisplayText()
        {
            return targetType switch
            {
                TargetType.Points => "Next Upgrade Point",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void Dispose()
        {
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencySpent, UpdateCurrentValue);
            SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencyAdded, UpdateCurrentValue);
        }
    }
}