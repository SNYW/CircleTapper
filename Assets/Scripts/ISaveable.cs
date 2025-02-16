using UnityEngine;

public interface ISaveable
{
    public BoardObjectSaveData ToSaveData();
    public void FromSaveData(BoardObjectSaveData saveData);
}

public abstract class BoardObjectSaveData
{
    public int level;
    public Vector2Int position;
}

public class CircleSaveData : BoardObjectSaveData
{
    public int curentValue;
    public int particlesToSpawn;
}

public class SquareSaveData : BoardObjectSaveData
{
    public int remainingCooldown;
}