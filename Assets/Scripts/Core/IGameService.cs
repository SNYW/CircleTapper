namespace Core
{
    /// <summary>
    /// Marker for anything that can live in the <see cref="ServiceLocator"/>.
    /// <para>
    /// Services are plain C# objects. They have no Unity lifecycle of their own, so anything
    /// needing per-frame work implements <see cref="ITickable"/> and is driven by the
    /// <see cref="ServiceRunner"/>. Anything needing scene references or Unity callbacks belongs
    /// in a MonoBehaviour that calls into a service, not in the service itself.
    /// </para>
    /// </summary>
    public interface IGameService
    {
    }
}
