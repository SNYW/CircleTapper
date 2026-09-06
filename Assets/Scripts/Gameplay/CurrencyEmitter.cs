using ObjectPooling;
using UnityEngine;

/// <summary>
/// Pays out banked currency as pooled particles, a few at a time.
/// <para>
/// Circle and Hex had byte-identical copies of this. It holds the outstanding amount rather than
/// spawning it all at once, so a large payout streams out over a second or two instead of
/// flooding the pool in one frame.
/// </para>
/// </summary>
public class CurrencyEmitter
{
    /// <summary>Above this backlog, particles are worth ten each so the queue drains sensibly.</summary>
    private const int BulkThreshold = 50;
    private const int BulkValue = 10;
    private const float ScatterRadius = 0.1f;

    /// <summary>Currency banked but not yet handed to the player.</summary>
    public int Pending { get; private set; }

    public bool HasPending => Pending > 0;

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Pending += amount;
    }

    public void Clear() => Pending = 0;

    /// <summary>
    /// Spawns one particle if anything is owed. Returns false when there was nothing to pay out,
    /// which is the common case.
    /// </summary>
    public bool TryEmit(Vector3 origin)
    {
        if (Pending <= 0) return false;

        int value = Pending > BulkThreshold ? BulkValue : 1;

        var particle = ObjectPoolManager
            .GetPool(ObjectPool.ObjectPoolName.CurrencyParticle)
            .GetPooledObject()
            .GetComponent<CurrencyParticle>();

        particle.transform.position = origin + new Vector3(
            Random.Range(-ScatterRadius, ScatterRadius),
            Random.Range(-ScatterRadius, ScatterRadius),
            0f);

        particle.gameObject.SetActive(true);
        particle.OnInit(value);

        Pending -= value;
        return true;
    }
}
