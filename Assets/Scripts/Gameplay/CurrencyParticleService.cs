using System.Collections.Generic;
using Core;
using ObjectPooling;
using UnityEngine;

/// <summary>
/// Flies every in-flight currency particle from the board to the HUD, in one pass.
/// <para>
/// Each particle used to run its own UniTask loop. One loop for the system beats N loops for N
/// objects: no per-particle state machine, one contiguous list to walk, and a single place to
/// cap and budget the work.
/// </para>
/// <para>
/// This is the legitimate use of <see cref="ITickable"/> — genuinely per-frame work across many
/// objects, not a delay loop wearing a tick costume. See CLAUDE.md.
/// </para>
/// </summary>
public class CurrencyParticleService : IGameService, ITickable
{
    /// <summary>
    /// Ceiling on particles in the air. Past this the emitter banks instead of spawning, and the
    /// backlog comes out as higher-value particles — same currency, fewer objects, and identical
    /// on screen. This is the primary cost lever; throttling below is only a safety valve.
    /// </summary>
    private const int MaxConcurrent = 250;

    /// <summary>
    /// Particles moved per frame before staggering kicks in. Skipped particles bank their
    /// delta time and spend it when their turn comes, so distance travelled is unchanged and
    /// only smoothness degrades — acceptable at counts where the screen is already busy.
    /// </summary>
    private const int UpdateBudget = 120;

    private const float ArrivalDistance = 0.1f;

    private readonly List<Flight> _flights = new(MaxConcurrent);

    private Transform _anchor;
    private Camera _camera;
    private int _cursor;

    public int LiveCount => _flights.Count;

    public bool HasCapacity => _flights.Count < MaxConcurrent;

    /// <summary>
    /// Sends one particle on its way. False when at capacity or the HUD anchor is missing, in
    /// which case the caller should keep the currency banked rather than dropping it.
    /// </summary>
    public bool TryLaunch(Vector3 origin, int value)
    {
        if (!HasCapacity) return false;

        var particle = ObjectPoolManager
            .GetPool(ObjectPool.ObjectPoolName.CurrencyParticle)
            .GetPooledObject()
            .GetComponent<CurrencyParticle>();

        if (!TryResolveTarget(particle, out Vector3 target)) return false;

        particle.transform.position = origin;
        particle.gameObject.SetActive(true);
        particle.Prepare(value);

        Vector3 toTarget = target - origin;
        particle.transform.rotation =
            Quaternion.Euler(0f, 0f, Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg);

        _flights.Add(new Flight
        {
            Particle = particle,
            Target = target,
            TotalDistance = Mathf.Max(toTarget.magnitude, ArrivalDistance),
            Arc = Random.Range(-particle.arcHeight, particle.arcHeight),
            Speed = particle.moveSpeed,
            BankedTime = 0f
        });

        return true;
    }

    public void Tick(float deltaTime)
    {
        int count = _flights.Count;
        if (count == 0) return;

        // Everyone banks the frame's time; only some of them get to spend it this frame.
        for (int i = 0; i < count; i++)
        {
            Flight flight = _flights[i];
            flight.BankedTime += deltaTime;
            _flights[i] = flight;
        }

        int toMove = Mathf.Min(count, UpdateBudget);
        for (int n = 0; n < toMove; n++)
        {
            if (_cursor >= _flights.Count) _cursor = 0;

            if (Advance(_cursor)) RemoveAt(_cursor);
            else _cursor++;
        }
    }

    /// <summary>Returns true once the particle has arrived and should be retired.</summary>
    private bool Advance(int index)
    {
        Flight flight = _flights[index];
        Transform particleTransform = flight.Particle.transform;

        Vector3 position = Vector3.MoveTowards(
            particleTransform.position, flight.Target, flight.Speed * flight.BankedTime);

        flight.BankedTime = 0f;

        // One distance check per frame. The original took two square roots for the same answer.
        float remaining = Vector3.Distance(position, flight.Target);

        particleTransform.position = position;

        float progress = 1f - remaining / flight.TotalDistance;
        float height = flight.Arc * 4f * (progress - 0.5f) * (progress - 0.5f);
        flight.Particle.SetArcOffset(flight.Arc - height);

        _flights[index] = flight;

        return remaining <= ArrivalDistance;
    }

    private void RemoveAt(int index)
    {
        _flights[index].Particle.ReturnToPool();

        // Swap with the last so removal stays O(1); the cursor stays put and picks up whatever
        // moved into this slot next frame.
        int last = _flights.Count - 1;
        _flights[index] = _flights[last];
        _flights.RemoveAt(last);
    }

    private bool TryResolveTarget(CurrencyParticle particle, out Vector3 target)
    {
        target = default;

        if (_anchor == null)
        {
            GameObject found = GameObject.Find(particle.anchorName);
            if (found == null)
            {
                Debug.LogError($"No currency anchor named '{particle.anchorName}'.");
                return false;
            }

            _anchor = found.transform;
        }

        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return false;

        target = _camera.ScreenToWorldPoint(_anchor.position);
        target.z = 0f;
        return true;
    }

    private struct Flight
    {
        public CurrencyParticle Particle;
        public Vector3 Target;
        public float TotalDistance;
        public float Arc;
        public float Speed;

        /// <summary>Time accrued but not yet applied, so staggering never loses distance.</summary>
        public float BankedTime;
    }
}
