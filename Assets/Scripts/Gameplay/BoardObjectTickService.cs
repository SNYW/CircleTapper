using System.Collections.Generic;
using Core;

/// <summary>
/// Ticks every live board object from one place.
/// <para>
/// Circle, Square and Hex each had their own Update, so a full board meant ~126 managed-to-native
/// callbacks a frame, most of which early-returned immediately. One loop over a list costs a
/// fraction of that and makes the work visible in one profiler entry instead of scattered across
/// a hundred components.
/// </para>
/// <para>
/// This registry is a natural fit to fold into GridService when that lands — the grid already
/// knows which cells hold what.
/// </para>
/// </summary>
public class BoardObjectTickService : IGameService, ITickable
{
    private readonly List<BoardObject> _live = new();

    public int LiveCount => _live.Count;

    /// <summary>Called from OnEnable. Unity pairs OnEnable/OnDisable, so this cannot double up.</summary>
    public void Register(BoardObject boardObject)
    {
        if (boardObject == null) return;

        _live.Add(boardObject);
    }

    public void Unregister(BoardObject boardObject)
    {
        int index = _live.IndexOf(boardObject);
        if (index < 0) return;

        RemoveAt(index);
    }

    public void Tick(float deltaTime)
    {
        // Backwards, so an object retiring itself mid-tick cannot shuffle the ones still to run.
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            BoardObject boardObject = _live[i];

            // Destroyed without OnDisable reaching us — drop it rather than throwing.
            if (boardObject == null)
            {
                RemoveAt(i);
                continue;
            }

            boardObject.Tick(deltaTime);
        }
    }

    private void RemoveAt(int index)
    {
        int last = _live.Count - 1;
        _live[index] = _live[last];
        _live.RemoveAt(last);
    }
}
