using System.Collections.Generic;
using Core;

/// <summary>
/// Relays every circle completion to every triangle on the board.
/// <para>
/// Triangles charge from board-wide activity rather than adjacency, so unlike the hex — which is
/// told directly by the circle next to it — they genuinely want all of them. That makes this
/// O(triangles) of useful work rather than the O(hexes) of wasted filtering the old global
/// CircleComplete event used to do.
/// </para>
/// </summary>
public class TriangleChargeService : IGameService
{
    private readonly List<Triangle> _triangles = new();

    public int Count => _triangles.Count;

    public void Register(Triangle triangle)
    {
        if (triangle == null) return;

        _triangles.Add(triangle);
    }

    public void Unregister(Triangle triangle)
    {
        int index = _triangles.IndexOf(triangle);
        if (index < 0) return;

        int last = _triangles.Count - 1;
        _triangles[index] = _triangles[last];
        _triangles.RemoveAt(last);
    }

    public void NotifyCircleCompleted(Circle circle)
    {
        // Backwards: firing a beam can complete another circle, which re-enters this method.
        for (int i = _triangles.Count - 1; i >= 0; i--)
        {
            Triangle triangle = _triangles[i];
            if (triangle == null) continue;

            triangle.OnCircleCompletedAnywhere(circle);
        }
    }
}
