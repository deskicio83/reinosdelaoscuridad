namespace ReinoOscuridad.Data
{
    // ── Structs de datos para los eventos del EventBus ────────────────────────
    // Solo datos planos. Sin lógica. Coinciden 1:1 con los eventos de EventBus.

    public struct PlayerLevelUpData
    {
        public int previousLevel;
        public int newLevel;
        /// Alias en español para consistencia con el resto de eventos
        public int nuevoNivel;
        /// Features desbloqueadas en este nivel (puede estar vacío)
        public string[] desbloqueos;
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
        /// heroId del héroe afectado, o null para operaciones de inventario
        public string heroId;
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
        /// Puntos de Pase Oscuro que otorga esta misión (0 si ninguno)
        public int pasePoints;
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

    // ── Eventos de combate ────────────────────────────────────────────────────

    public struct CombatTurnEndData
    {
        public int  turnoActual;
        /// true si el equipo del jugador acaba de actuar
        public bool esEquipoJugador;
    }

    public struct UnitDamagedData
    {
        /// "hero" | "enemy"
        public string unitType;
        public string unitId;
        public DamageResult resultado;
        public int  hpActual;
        public int  hpMax;
    }

    public struct UnitDefeatedData
    {
        /// "hero" | "enemy"
        public string unitType;
        public string unitId;
    }

    public struct EffectAppliedData
    {
        /// "hero" | "enemy"
        public string unitType;
        public string unitId;
        public string efectoId;
        /// Stacks totales del efecto en el objetivo tras la aplicación
        public int    stacks;
    }

    // ── Eventos de progresión del jugador ─────────────────────────────────────

    public struct PaseLevelUpData
    {
        /// Nuevo nivel del Pase Oscuro alcanzado
        public int nuevoNivel;
        /// true si el jugador tiene el carril premium activo
        public bool esCarrilPremium;
    }

    // ── Eventos de progresión de héroes ───────────────────────────────────────

    public struct HeroLevelUpData
    {
        public string heroId;
        public int    nuevoNivel;
    }

    public struct HeroAwakenedData
    {
        public string heroId;
        /// Estrellas tras el awaken (1–6)
        public int    nuevasEstrellas;
    }
}
