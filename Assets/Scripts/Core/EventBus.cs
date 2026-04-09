using System;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Core
{
    /// Bus de eventos central. Los sistemas suscriben y publican aquí.
    /// Ningún sistema llama a otro sistema directamente.
    public static class EventBus
    {
        // ── Declaración de eventos ─────────────────────────────────────────────

        public static event Action<PlayerLevelUpData>    OnPlayerLevelUp;
        public static event Action<CurrencyChangedData>  OnCurrencyChanged;
        public static event Action<HeroAddedData>        OnHeroAdded;
        public static event Action<GearChangedData>      OnGearChanged;
        public static event Action<CombatCompletedData>  OnCombatCompleted;
        public static event Action<MissionCompletedData> OnMissionCompleted;
        public static event Action<DailyResetData>       OnDailyReset;

        // ── Métodos de publicación ─────────────────────────────────────────────

        public static void Publish(PlayerLevelUpData data)    => OnPlayerLevelUp?.Invoke(data);
        public static void Publish(CurrencyChangedData data)  => OnCurrencyChanged?.Invoke(data);
        public static void Publish(HeroAddedData data)        => OnHeroAdded?.Invoke(data);
        public static void Publish(GearChangedData data)      => OnGearChanged?.Invoke(data);
        public static void Publish(CombatCompletedData data)  => OnCombatCompleted?.Invoke(data);
        public static void Publish(MissionCompletedData data) => OnMissionCompleted?.Invoke(data);
        public static void Publish(DailyResetData data)       => OnDailyReset?.Invoke(data);

        /// Elimina todos los suscriptores. Llamar en tests o al reiniciar la sesión.
        public static void ClearAll()
        {
            OnPlayerLevelUp    = null;
            OnCurrencyChanged  = null;
            OnHeroAdded        = null;
            OnGearChanged      = null;
            OnCombatCompleted  = null;
            OnMissionCompleted = null;
            OnDailyReset       = null;
        }
    }
}
