namespace ReinoOscuridad.Data
{
    // ── Structs de datos para los eventos del EventBus ────────────────────────
    // Solo datos planos. Sin lógica. Coinciden 1:1 con los eventos de EventBus.

    public struct PlayerLevelUpData
    {
        public int previousLevel;
        public int newLevel;
    }

    public struct CurrencyChangedData
    {
        /// "oroNegro", "caosifera", "summonScrolls", "ascensionStones"
        public string currency;
        /// Delta aplicado (positivo = ganancia, negativo = gasto)
        public int delta;
        public int newAmount;
    }

    public struct HeroAddedData
    {
        public string heroId;
        /// true si ya existía en el inventario (duplicado de gacha)
        public bool wasDuplicate;
    }

    public struct GearChangedData
    {
        /// ID de instancia única de la pieza
        public string instanceId;
        /// ID del tipo de gear en el catálogo
        public string gearId;
        /// "equipped", "unequipped", "upgraded", "sold"
        public string changeType;
    }

    public struct CombatCompletedData
    {
        public string encounterId;
        public bool victory;
        public int expGained;
        public int goldGained;
    }

    public struct MissionCompletedData
    {
        public string missionId;
        /// "daily", "weekly", "story"
        public string missionType;
    }

    public struct DailyResetData
    {
        /// Unix timestamp UTC del momento del reset detectado
        public long resetTimestamp;
    }

    public struct AuthStateChangedData
    {
        public string uid;
        /// true si la cuenta es anónima (invitado)
        public bool isGuest;
    }
}
