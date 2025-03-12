using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public interface ISaveable
{
    public BoardObjectSaveData ToSaveData();
    public void FromSaveData(BoardObjectSaveData saveData);
}

public enum BoardObjectType
{
    Circle,
    Square,
    Hex
}

[Serializable]
public class BoardObjectSaveData
{
    public string type;
    public int level;
    public int xPosition;
    public int yPosition;
    public int value;
    public int carryoverValue;
}