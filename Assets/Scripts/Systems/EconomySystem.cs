using System;
using UnityEngine;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Systems
{
    /// Gestiona energía, oro negro y caosifera en memoria.
    /// Todos los consumos y ganancias de recursos pasan por aquí.
    /// NUNCA escribe a Firestore — solo actualiza memoria y publica eventos.
    [DefaultExecutionOrder(-25)]
    public class EconomySystem : MonoBehaviour, ISystem
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static EconomySystem Instance { get; private set; }

        // ── Constantes ─────────────────────────────────────────────────────────

        /// Segundos por unidad de energía regenerada (4 minutos).
        public const int ENERGY_REGEN_SECONDS = 240;

        // ── Estado en memoria ──────────────────────────────────────────────────

        public int CurrentEnergy  { get; private set; }
        public int MaxEnergy      { get; private set; }
        public int CurrentGold    { get; private set; }
        public int CurrentCaosifera { get; private set; }

        private PlayerDataSystem _pds;

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

            // PlayerDataSystem tiene DefaultExecutionOrder(-50), así que
            // ya está registrado cuando EconomySystem llega aquí (-25).
            GameManager.Instance.RegisterSystem(this);
        }

        // ── ISystem ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            _pds = GameManager.Instance.GetSystem<PlayerDataSystem>();

            if (_pds == null)
            {
                Debug.LogError("[EconomySystem] PlayerDataSystem no encontrado. No se puede inicializar.");
                return;
            }

            var pd = _pds.GetPlayerData();

            MaxEnergy       = pd.energiaMax;
            CurrentGold     = pd.oroNegro;
            CurrentCaosifera = pd.caosifera;

            // Calcular energía acumulada offline
            int baseEnergy  = pd.energia;
            int offlineGain = CalculateOfflineEnergyGain(pd.lastLoginTimestamp);
            CurrentEnergy   = Mathf.Min(baseEnergy + offlineGain, MaxEnergy);

            if (offlineGain > 0)
            {
                Debug.Log($"[EconomySystem] Energía offline: +{offlineGain} unidades ({offlineGain * ENERGY_REGEN_SECONDS / 60} min offline). Total: {CurrentEnergy}/{MaxEnergy}");
                // Persiste el nuevo valor de energía
                pd.energia = CurrentEnergy;
                _pds.UpdatePlayerData(pd);
            }

            Debug.Log($"[EconomySystem] Init — Energía: {CurrentEnergy}/{MaxEnergy} · Oro: {CurrentGold} · Caosifera: {CurrentCaosifera}");
        }

        public void OnSessionStart() { }

        public void OnSessionEnd() { }

        // ── ENERGÍA ───────────────────────────────────────────────────────────

        /// Consume <amount> unidades de energía. Devuelve false si no hay suficiente.
        public bool ConsumeEnergy(int amount)
        {
            if (amount <= 0 || CurrentEnergy < amount) return false;

            CurrentEnergy -= amount;
            SyncAndNotify("energia", -amount, CurrentEnergy);
            return true;
        }

        /// Añade energía hasta el máximo.
        public void AddEnergy(int amount)
        {
            if (amount <= 0) return;

            int prev = CurrentEnergy;
            CurrentEnergy = Mathf.Min(CurrentEnergy + amount, MaxEnergy);
            int delta = CurrentEnergy - prev;

            if (delta > 0)
                SyncAndNotify("energia", delta, CurrentEnergy);
        }

        // ── ORO NEGRO ─────────────────────────────────────────────────────────

        public bool ConsumeGold(int amount)
        {
            if (amount <= 0 || CurrentGold < amount) return false;

            CurrentGold -= amount;
            SyncAndNotify("oroNegro", -amount, CurrentGold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            CurrentGold += amount;
            SyncAndNotify("oroNegro", amount, CurrentGold);
        }

        // ── CAOSIFERA ────────────────────────────────────────────────────────

        public bool ConsumeCaosifera(int amount)
        {
            if (amount <= 0 || CurrentCaosifera < amount) return false;

            CurrentCaosifera -= amount;
            SyncAndNotify("caosifera", -amount, CurrentCaosifera);
            return true;
        }

        public void AddCaosifera(int amount)
        {
            if (amount <= 0) return;

            CurrentCaosifera += amount;
            SyncAndNotify("caosifera", amount, CurrentCaosifera);
        }

        // ── Helpers privados ───────────────────────────────────────────────────

        /// Calcula unidades de energía ganadas offline desde <lastLoginTimestamp> hasta ahora.
        private static int CalculateOfflineEnergyGain(long lastLoginTimestamp)
        {
            long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsedSeconds = nowUtc - lastLoginTimestamp;
            if (elapsedSeconds <= 0) return 0;
            return (int)(elapsedSeconds / ENERGY_REGEN_SECONDS);
        }

        /// Sincroniza el campo <currency> en PlayerDataSystem y publica OnCurrencyChanged.
        private void SyncAndNotify(string currency, int delta, int newAmount)
        {
            var pd = _pds.GetPlayerData();

            switch (currency)
            {
                case "energia":    pd.energia   = CurrentEnergy;   break;
                case "oroNegro":   pd.oroNegro  = CurrentGold;     break;
                case "caosifera":  pd.caosifera = CurrentCaosifera; break;
            }

            _pds.UpdatePlayerData(pd);

            EventBus.Publish(new CurrencyChangedData
            {
                currency  = currency,
                delta     = delta,
                newAmount = newAmount
            });
        }
    }
}
