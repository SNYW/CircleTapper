using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        public static bool DEBUGMODE = false;

        public int passiveBonus;
        public BoardObject defaultStartingObject;
        public InWorldGridManager gridManager;

        public List<Circle> circleLevels;
        public List<Square> squareLevels;
        public List<Hex> hexLevels;
    
        private void Awake()
        {
            
#if !UNITY_EDITOR
            Application.targetFrameRate = 60;
#endif
            
            DontDestroyOnLoad(transform.parent.gameObject);
            DOTween.Init();
            SystemEventManager.Init();
            PurchaseManager.Init();
            SoundManager.Init();
            ObjectiveManager.Init();
        
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoad;
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.buildIndex != 1) return;
            gridManager.gameObject.SetActive(true);
            StartCoroutine(GivePassiveIncome());
            SaveManager.Instance.Init(this);
            FindAnyObjectByType<CameraZoomController>().OnGameplayStart();
        }

        private IEnumerator GivePassiveIncome()
        {
            while (gameObject.activeSelf)
            {
                yield return new WaitForSeconds(1);
                var boardItems = FindObjectsByType<BoardObject>(FindObjectsSortMode.None);
                SystemEventManager.Send(SystemEventManager.GameEvent.CurrencyAdded, boardItems.Length+passiveBonus);
            }
        }

        public void ToggleDebug()
        {
            DEBUGMODE = !DEBUGMODE;
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space)) ToggleDebug();
        }

        private void OnDisable()
        {
            PurchaseManager.Dispose();
        }

        public void ResetOnLoad()
        {
            var objects = FindObjectsByType<BoardObject>(FindObjectsSortMode.None);
            foreach (var boardObject in objects)
            {
                Destroy(boardObject.gameObject);
            }
        }
    }
}
