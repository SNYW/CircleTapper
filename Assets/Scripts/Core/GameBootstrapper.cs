using Economy;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Persistence;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Single entry point for the game's systems. Registration is deliberately manual and lives
    /// in one method so startup order reads top to bottom.
    /// <para>
    /// Startup is two phases. Registration happens in Awake, so anything running from Start can
    /// resolve a service. Async initialization finishes later — consumers needing loaded data
    /// must wait for <see cref="Ready"/> rather than reading in Start.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrapper : MonoBehaviour
    {
        [Tooltip("Keeps services alive across scene loads. Leave on unless this is a test scene.")]
        [SerializeField] private bool persistAcrossScenes = true;

        private static GameBootstrapper _instance;

        /// <summary>True once every service has finished async initialization.</summary>
        public static bool IsReady { get; private set; }

        /// <summary>
        /// Fires when initialization completes. Subscribing late still works — handlers added
        /// once <see cref="IsReady"/> is true are invoked immediately.
        /// </summary>
        public static event Action Ready
        {
            add
            {
                if (IsReady) value?.Invoke();
                else ReadyInternal += value;
            }
            remove => ReadyInternal -= value;
        }

        private static event Action ReadyInternal;

        private ServiceRunner _runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            _instance = null;
            IsReady = false;
            ReadyInternal = null;
        }

        /// <summary>
        /// Creates the bootstrapper before the first scene loads, so services are registered no
        /// matter which scene is entered — including when a developer hits play straight into
        /// the gameplay scene. Placing one in a scene by hand still works; the singleton guard
        /// keeps whichever arrives first.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (_instance != null) return;

            var host = new GameObject(nameof(GameBootstrapper));
            host.AddComponent<GameBootstrapper>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

            RegisterServices();
        }

        private void Start()
        {
            // Cancels when this object is destroyed, which covers exiting play mode.
            BootstrapAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// The one place services are wired up. Order matters: a service may depend on anything
        /// registered above it, both here and during async initialization.
        /// </summary>
        private void RegisterServices()
        {
            ServiceLocator.Register(new SaveService());
            ServiceLocator.Register(new CurrencyService(ServiceLocator.Get<SaveService>()));
        }

        private async UniTaskVoid BootstrapAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (IGameService service in ServiceLocator.All)
                {
                    if (service is IAsyncInitializable initializable)
                    {
                        await initializable.InitializeAsync(cancellationToken);
                    }
                }

                _runner = gameObject.AddComponent<ServiceRunner>();
                _runner.Bind(ServiceLocator.All);

                IsReady = true;
                ReadyInternal?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Play mode exited or the bootstrapper was destroyed mid-init. Nothing to report.
            }
            catch (Exception exception)
            {
                Debug.LogError($"Service initialization failed — the game is not in a playable state.\n{exception}");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_instance != this) return;

            foreach (IGameService service in ServiceLocator.All)
            {
                if (service is IApplicationLifecycle listener) listener.OnApplicationPaused(pauseStatus);
            }
        }

        private void OnApplicationQuit()
        {
            if (_instance != this) return;

            foreach (IGameService service in ServiceLocator.All)
            {
                if (service is IApplicationLifecycle listener) listener.OnApplicationQuitting();
            }
        }

        private void OnDestroy()
        {
            if (_instance != this) return;

            DisposeServices();
            ServiceLocator.Clear();

            IsReady = false;
            ReadyInternal = null;
            _instance = null;
        }

        private void DisposeServices()
        {
            IReadOnlyList<IGameService> services = ServiceLocator.All;
            for (int i = services.Count - 1; i >= 0; i--)
            {
                if (services[i] is IServiceDisposable disposable) disposable.DisposeService();
            }
        }
    }
}
