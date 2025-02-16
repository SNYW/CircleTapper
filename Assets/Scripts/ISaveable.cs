using System;
using UnityEngine;
using UnityEngine.Serialization;

public interface ISaveable
{
    public BoardObjectSaveData ToSaveData();
    public void FromSaveData(BoardObjectSaveData saveData);
}

[Serializable]
public abstract class BoardObjectSaveData
{
    public int level;
    public int xPosition;
    public int yPosition;
}

[Serializable]
public class CircleSaveData : BoardObjectSaveData
{
    public int currentValue;
    public int particlesToSpawn;
}

[Serializable]
public class SquareSaveData : BoardObjectSaveData
{
    public int remainingCooldown;
}