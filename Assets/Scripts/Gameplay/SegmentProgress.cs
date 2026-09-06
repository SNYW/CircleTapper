using System.Globalization;
using UnityEngine;

/// <summary>
/// The segmented-ring fill every board object draws, and the eased animation towards a new value.
/// <para>
/// Circle, Square and Hex each had their own identical copy of this: the same
/// <see cref="MaterialPropertyBlock"/> setup, the same two floats, and the same
/// <see cref="Mathf.MoveTowards"/> in Update. One copy means fixing the batching cost or the
/// animation is a single edit rather than three.
/// </para>
/// </summary>
public class SegmentProgress
{
    private static readonly int RemovedSegmentsId = Shader.PropertyToID("_RemovedSegments");
    private static readonly int SegmentCountId = Shader.PropertyToID("_SegmentCount");

    private const float LerpSpeed = 10f;

    private readonly SpriteRenderer _renderer;
    private readonly MaterialPropertyBlock _block;

    private float _current;

    public SegmentProgress(SpriteRenderer renderer, int segmentCount, float initial)
    {
        _renderer = renderer;
        _block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_block);

        _block.SetFloat(SegmentCountId, segmentCount);

        _current = initial;
        Target = initial;
        Apply();
    }

    /// <summary>Where the fill is animating to. Set it; the object ticks its way there.</summary>
    public float Target { get; set; }

    public float Current => _current;

    /// <summary>True once the ring is completely removed — a circle's completion condition.</summary>
    public bool IsFull => Mathf.Approximately(_current, 1f);

    public bool IsAnimating => !Mathf.Approximately(_current, Target);

    /// <summary>
    /// Steps towards <see cref="Target"/>. Returns false when there was nothing to do, so callers
    /// can skip the renderer write entirely — this used to run every frame regardless.
    /// </summary>
    public bool Advance(float deltaTime)
    {
        if (!IsAnimating) return false;

        _current = Mathf.MoveTowards(_current, Target, deltaTime * LerpSpeed);
        Apply();
        return true;
    }

    /// <summary>Jumps straight to a value, with no animation.</summary>
    public void SetImmediate(float value)
    {
        _current = value;
        Target = value;
        Apply();
    }

    public string ToDebugString() => _current.ToString(CultureInfo.InvariantCulture);

    private void Apply()
    {
        _block.SetFloat(RemovedSegmentsId, _current);
        _renderer.SetPropertyBlock(_block);
    }
}
