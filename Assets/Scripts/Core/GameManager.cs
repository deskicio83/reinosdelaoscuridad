using System;
using System.Collections.Generic;
using UnityEngine;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Core
{
    /// Singleton DontDestroyOnLoad. Orquesta el ciclo de vida de todos los sistemas.
    /// NO accede a Firestore. NO contiene lógica de negocio.
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        // ── Estado interno ─────────────────────────────────────────────────────

        private readonly List<ISystem> _systems = new List<ISystem>();
        private bool _initialized;

        // ── Ciclo de vida Unity ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystems();
        }

        private void OnApplicationQuit()
        {
            NotifySessionEnd();
        }

        private void OnApplicationPause(bool paused)
        {
            // En móvil, la pausa equivale a salir — persistir datos
            if (paused) NotifySessionEnd();
        }

        // ── Registro de sistemas ───────────────────────────────────────────────

        /// Registra un sistema e inicializalo si GameManager ya arrancó.
        public void RegisterSystem<T>(T system) where T : ISystem
        {
            if (_systems.Contains(system)) return;
            _systems.Add(system);

            if (_initialized)
                system.Initialize();
        }

        /// Devuelve el primer sistema del tipo T registrado, o null si no existe.
        /// Ignora referencias destruidas (MonoBehaviour destruido = Unity null).
        public T GetSystem<T>() where T : class, ISystem
        {
            foreach (var s in _systems)
            {
                if (s is T typed)
                {
                    if (s is MonoBehaviour mb && mb == null) continue;
                    return typed;
                }
            }
            return null;
        }

        // ── Ciclo de sesión ────────────────────────────────────────────────────

        /// Llamado por PlayerDataSystem tras cargar los datos del jugador desde Firestore.
        /// Notifica a todos los sistemas que la sesión ha comenzado y detecta daily reset.
        public void NotifySessionStart(long lastLoginTimestamp)
        {
            foreach (var s in _systems)
                s.OnSessionStart();

            TryFireDailyReset(lastLoginTimestamp);
        }

        // ── Daily reset ────────────────────────────────────────────────────────

        /// Publica OnDailyReset si ha cruzado medianoche UTC desde el último login.
        /// GameManager no accede a Firestore — recibe el timestamp del PlayerDataSystem.
        public void TryFireDailyReset(long lastLoginTimestamp)
        {
            long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long midnightToday = GetMidnightUtcTimestamp(nowUtc);

            if (lastLoginTimestamp < midnightToday)
            {
                EventBus.Publish(new DailyResetData { resetTimestamp = midnightToday });
            }
        }

        // ── Helpers privados ───────────────────────────────────────────────────

        private void InitializeSystems()
        {
            foreach (var s in _systems)
                s.Initialize();

            _initialized = true;
        }

        private void NotifySessionEnd()
        {
            foreach (var s in _systems)
                s.OnSessionEnd();
        }

        /// Devuelve el Unix timestamp de la medianoche UTC del día que contiene <unixUtc>.
        private static long GetMidnightUtcTimestamp(long unixUtc)
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(unixUtc).UtcDateTime;
            var midnight = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
            return new DateTimeOffset(midnight).ToUnixTimeSeconds();
        }
    }
}
