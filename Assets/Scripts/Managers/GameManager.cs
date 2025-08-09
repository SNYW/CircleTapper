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
        public static bool DEBUGMODE;

        public int passiveBonus;
        public int passiveUpgradeBonus;
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
            UpgradeManager.Init();
        
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
               
                PurchaseManager.AddCurrency(PurchaseManager.GetPassiveIncomeAmount()+passiveBonus);
                PurchaseManager.AddUpgradePoints(passiveUpgradeBonus);
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
