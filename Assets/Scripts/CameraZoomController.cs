using Core;
using System;
using DG.Tweening;
using Persistence;
using UnityEngine;

public class CameraZoomController : MonoBehaviour
{
    public Vector2 zoomMinMax;
    public float zoomPerCell;
    public  Camera targetCam;
    private void Awake()
    {
        targetCam = Camera.main;
        SystemEventManager.Subscribe(SystemEventManager.GameEvent.GridCellUnlocked, OnCellUnlocked);
    }

    public void OnGameplayStart()
    {
        targetCam.orthographicSize = GetCamSize();
    }

    private void OnCellUnlocked(object obj)
    {
        // Kill only this camera's tween. DOTween.KillAll takes a bool, not a target, so the
        // old call compiled via UnityEngine.Object's implicit bool and killed every tween in
        // the game. See CLAUDE.md.
        targetCam.DOKill();
        targetCam.DOOrthoSize(GetCamSize(), 0.2f).SetEase(Ease.InQuad).SetLink(gameObject);
    }

    private float GetCamSize()
    {
        var cellCount = ServiceLocator.Get<SaveService>().Data.unlockedCells.Count;
        return  Mathf.Clamp(zoomMinMax.x + zoomPerCell * cellCount, zoomMinMax.x, zoomMinMax.y);
    }

    private void OnDisable()
    {
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.GridCellUnlocked, OnCellUnlocked);
    }
}
