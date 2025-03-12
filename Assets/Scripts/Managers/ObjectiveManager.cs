using System;
using System.Collections.Generic;
using Objectives;
using Persistence;
using UnityEngine;

namespace Managers
{
    public static class ObjectiveManager
    {
        private static List<Objective> _objectives;

        public static Objective CurrentObjective;
        public static void Init()
        {
            _objectives = new List<Objective>();
            TextAsset file = Resources.Load<TextAsset>("Data/Objectives/Objectives");
            if (file == null)
            {
                Debug.LogError("Objectives File not found");
            }

            var stringObjectives = file.text.Split(Environment.NewLine);

            foreach (var text in stringObjectives)
            {
                var split = text.Split(',');

                _objectives.Add(new Objective(split[0], split[1], split[2]));
            }
        }

        public static void OnGameLoad(GameData gameData)
        {
            if (string.IsNullOrEmpty(gameData.currentObjective))
            {
                CurrentObjective = _objectives[0];
                Debug.Log($"Current Objective Loaded as: {CurrentObjective.id}");
                SystemEventManager.Send(SystemEventManager.GameEvent.ObjectiveUpdated, CurrentObjective);
                return;
            }

            CurrentObjective = _objectives.Find(o => o.id == gameData.currentObjective);
            Debug.Log($"Current Objective Loaded as: {CurrentObjective.id}");
            
            SystemEventManager.Send(SystemEventManager.GameEvent.ObjectiveUpdated, CurrentObjective);
        }
        public static void ClaimObjective()
        {
            if (_objectives[^1] == CurrentObjective) return;
            
            
            var objectiveIndex = _objectives.IndexOf(CurrentObjective);
            CurrentObjective.Dispose();
            CurrentObjective = _objectives[objectiveIndex + 1];
            SystemEventManager.Send(SystemEventManager.GameEvent.ObjectiveUpdated, CurrentObjective);
        }

        public static void ResetObjectives()
        {
            CurrentObjective = _objectives[0];
            SystemEventManager.Send(SystemEventManager.GameEvent.ObjectiveUpdated, CurrentObjective);
        }
    }
}