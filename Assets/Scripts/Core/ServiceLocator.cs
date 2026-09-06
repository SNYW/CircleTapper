using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Registry of long-lived game systems, resolved by type.
    /// <para>
    /// Services are registered once by <see cref="GameBootstrapper"/> and only read from
    /// afterwards. Each service is a single responsibility with a single implementation, so the
    /// concrete type is the key — <c>Register(new SaveService())</c> resolves via
    /// <c>Get&lt;SaveService&gt;()</c>.
    /// </para>
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IGameService> Services = new();
        private static readonly List<IGameService> RegistrationOrder = new();

        /// <summary>
        /// Services in registration order. Initialization and ticking follow this order;
        /// shutdown walks it backwards.
        /// </summary>
        public static IReadOnlyList<IGameService> All => RegistrationOrder;

        /// <summary>
        /// Statics survive between play sessions if domain reload is ever disabled, which would
        /// leave stale services registered on the second run.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            Services.Clear();
            RegistrationOrder.Clear();
        }

        public static void Register<TService>(TService service) where TService : class, IGameService
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            Type key = typeof(TService);
            if (Services.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"{key.Name} is already registered. Use Replace<{key.Name}>() if the swap is deliberate.");
            }

            Services.Add(key, service);
            if (!RegistrationOrder.Contains(service)) RegistrationOrder.Add(service);
        }

        /// <summary>Swaps an existing registration. For tests and editor tooling.</summary>
        public static void Replace<TService>(TService service) where TService : class, IGameService
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            if (Services.Remove(typeof(TService), out IGameService existing)
                && !IsRegisteredUnderAnyKey(existing))
            {
                RegistrationOrder.Remove(existing);
            }

            Register(service);
        }

        public static TService Get<TService>() where TService : class, IGameService
        {
            if (Services.TryGetValue(typeof(TService), out IGameService service)) return (TService)service;

            throw new ServiceNotFoundException(typeof(TService));
        }

        public static bool TryGet<TService>(out TService service) where TService : class, IGameService
        {
            if (Services.TryGetValue(typeof(TService), out IGameService found))
            {
                service = (TService)found;
                return true;
            }

            service = null;
            return false;
        }

        public static bool IsRegistered<TService>() where TService : class, IGameService
            => Services.ContainsKey(typeof(TService));

        public static void Clear()
        {
            Services.Clear();
            RegistrationOrder.Clear();
        }

        private static bool IsRegisteredUnderAnyKey(IGameService service)
        {
            foreach (KeyValuePair<Type, IGameService> pair in Services)
            {
                if (ReferenceEquals(pair.Value, service)) return true;
            }

            return false;
        }
    }

    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(Type serviceType)
            : base($"No service registered for {serviceType.Name}. Register it in GameBootstrapper, " +
                   "and check the bootstrapper ran first — services resolve from Awake, but only " +
                   "finish initializing once GameBootstrapper.IsReady is true.")
        {
        }
    }
}
