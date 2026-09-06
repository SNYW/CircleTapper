using Progression;
using Core;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class AutoTapper : MonoBehaviour
    {
        public float baseTapSpeed;
        public float minSpeed;
        public float initialDelay;
        private BoardObject targetObject;

        public List<CanvasGroup> canvasGroups;
        public Image progressCircle;

        private bool active;
        private float remainingCooldown;

        private void Awake()
        {
            foreach (var cg in canvasGroups)
            {
                cg.alpha = 0;
            }
            StopAutoTap();
        }

        private void FadeTo(float value)
        {
            foreach (var cg in canvasGroups)
            {
                DOTween.KillAll(true, cg);
                cg.DOFade(value, 0.2f);
            }
        }

        private float GetCooldown()
        {
            const string upgradeName = "Auto-Tap Speed";

            if (!ServiceLocator.Get<UpgradeCatalog>()
                    .TryGet(upgradeName, out AutoTapSpeedUpgradeDefinition def)) return baseTapSpeed;

            int level = ServiceLocator.Get<UpgradeService>().GetLevel(upgradeName);
            return Mathf.Clamp(baseTapSpeed - level * def.speedPerLevel, minSpeed, baseTapSpeed);
        }

        public void StartAutoTap()
        {
            StartCoroutine(InitialDelay());
        }

        private IEnumerator InitialDelay()
        {
            yield return new WaitForSeconds(initialDelay);
            active = true;
            FadeTo(1);
            remainingCooldown = GetCooldown();
        }

        private void Update()
        {
            if (!active) return;

            remainingCooldown -= Time.deltaTime;
            progressCircle.fillAmount = remainingCooldown / GetCooldown();
            if (remainingCooldown <= 0)
            {
                Tap();
            }
        }

        private void Tap()
        {
            var cell = GridManager.GetClosestCell(transform.position, true);
            if (cell != null && cell.heldObject is Circle c)
            {
               c.OnTap();
            }

            remainingCooldown = GetCooldown();
        }

        public void StopAutoTap()
        { 
            StopAllCoroutines();
            FadeTo(0);
            if (!active) return;
            active = false;
            remainingCooldown = GetCooldown();
        }
    }
}