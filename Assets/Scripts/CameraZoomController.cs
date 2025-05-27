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
        DOTween.KillAll(targetCam);
        targetCam.DOOrthoSize(GetCamSize(), 0.2f).SetEase(Ease.InQuad);
    }

    private float GetCamSize()
    {
        var cellCount = SaveManager.Instance.gameData.unlockedCells.Count;
        return  Mathf.Clamp(zoomMinMax.x + zoomPerCell * cellCount, zoomMinMax.x, zoomMinMax.y);
    }

    private void OnDisable()
    {
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.GridCellUnlocked, OnCellUnlocked);
    }
}
