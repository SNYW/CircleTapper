using System;
using System.Collections.Generic;

public static class SystemEventManager
{
       public enum GameEvent
       {
              CircleComplete,
              CurrencyAdded,
              CurrencySpent,
              BoardObjectMoved
       }
       private static Dictionary<GameEvent, Action<object>> _eventListeners;

       public static void Init()
       {
              _eventListeners = new Dictionary<GameEvent, Action<object>>();

              foreach (GameEvent gameEvent in Enum.GetValues(typeof(GameEvent)))
              {
                     _eventListeners[gameEvent] = delegate { };
              }
       }

       public static void Subscribe(GameEvent eventName, Action<object> action)
       {
              _eventListeners ??= new Dictionary<GameEvent, Action<object>>();

              if (!_eventListeners.TryGetValue(eventName, out var v)) return;

              if (action != null)
              {
                     _eventListeners[eventName] += action;
              }
       }
       
       public static void Unsubscribe(GameEvent eventName, Action<object> action)
       {
              _eventListeners ??= new Dictionary<GameEvent, Action<object>>();

              if (!_eventListeners.TryGetValue(eventName, out var v)) return;

              if (action != null)
              {
                     _eventListeners[eventName] -= action;
              }
       }

       public static void Send(GameEvent gameEvent, object payload)
       {
              if (_eventListeners != null && _eventListeners.TryGetValue(gameEvent, out var action))
              {
                     action?.Invoke(payload);
              }
       }
}