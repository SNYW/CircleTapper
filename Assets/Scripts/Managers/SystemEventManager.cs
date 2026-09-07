using System;
using System.Collections.Generic;

public static class SystemEventManager
{
       public enum GameEvent
       {
              CurrencyAdded,
              CurrencySpent,
              BoardObjectMoved,
              BoardChanged,
              ObjectDragged,
              ObjectDropped,
              GridCellUnlocked,
              UpgradePointAdded,
              UpgradePointSpent,
              GameLoaded
       }
       private static Dictionary<GameEvent, Action<object>> _eventListeners;

       /// <summary>
       /// Once the app is closing, nothing dispatched can be safely handled: Unity destroys
       /// objects in an unspecified order, so a listener may already be gone, or may reach for a
       /// grid, a text field or a service that is. Board objects fire BoardChanged from
       /// OnDestroy, which is exactly this case.
       /// </summary>
       private static bool _isShuttingDown;

       public static void Init()
       {
              _isShuttingDown = false;
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

       /// <summary>Stops dispatch for good. Called once the application is closing.</summary>
       public static void Shutdown() => _isShuttingDown = true;

       public static void Send(GameEvent gameEvent, object payload)
       {
              if (_isShuttingDown) return;

              if (_eventListeners != null && _eventListeners.TryGetValue(gameEvent, out var action))
              {
                     action?.Invoke(payload);
              }
       }
}