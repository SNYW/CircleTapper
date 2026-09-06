using Economy;
using Core;
using ObjectPooling;
using UnityEngine;

/// <summary>
/// A single currency particle. Deliberately dumb — <see cref="CurrencyParticleService"/> owns
/// the flight, so this holds only what the service needs to read and the payout on arrival.
/// </summary>
public class CurrencyParticle : PooledObject
{
    public string anchorName;
    public float moveSpeed;
    public float arcHeight;
    public Transform animateTransform;

    private int _value;

    public void Prepare(int value)
    {
        _value = value;
        animateTransform.localPosition = Vector3.zero;
    }

    /// <summary>Vertical offset that gives the flight its arc.</summary>
    public void SetArcOffset(float offset) => animateTransform.localPosition = new Vector3(0f, offset, 0f);

    public override void ReturnToPool()
    {
        ServiceLocator.Get<CurrencyService>().AddPoints(_value);
        _value = 0;
        base.ReturnToPool();
    }
}
