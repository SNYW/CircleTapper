using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core
{
    /// <summary>
    /// For services that must do work before the game is playable — loading saves, reading
    /// data tables. The bootstrapper awaits these in registration order, so a service may
    /// assume everything registered before it has already initialized.
    /// </summary>
    public interface IAsyncInitializable
    {
        UniTask InitializeAsync(CancellationToken cancellationToken);
    }

    /// <summary>Per-frame update, driven by <see cref="ServiceRunner"/> in registration order.</summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }

    /// <summary>
    /// For services holding something that must be released — file handles, event
    /// subscriptions, pending writes. Called in reverse registration order on shutdown.
    /// </summary>
    public interface IServiceDisposable
    {
        void DisposeService();
    }

    /// <summary>
    /// For services that must react to the app being backgrounded or closed — flushing pending
    /// writes, pausing timers. Forwarded by <see cref="GameBootstrapper"/>.
    /// <para>
    /// On mobile, <c>OnApplicationPaused(true)</c> is the reliable signal. Quit is often not
    /// delivered at all on iOS, so never rely on it alone for anything that must not be lost.
    /// </para>
    /// </summary>
    public interface IApplicationLifecycle
    {
        void OnApplicationPaused(bool paused);
        void OnApplicationQuitting();
    }
}
