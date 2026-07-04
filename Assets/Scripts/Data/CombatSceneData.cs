using System.Collections.Generic;
using System.Linq;

namespace ReinoOscuridad.Data
{
    /// Pasarela estática de datos entre Scenes de combate.
    /// Antes de navegar a CombatScene: asignar PendingContext.
    /// En CombatSceneController.Start(): leer PendingContext.
    /// Al terminar: SetResult() guarda el resultado y limpia PendingContext.
    public static class CombatSceneData
    {
        public static CombatContext PendingContext { get; set; }
        public static CombatContext LastContext    { get; private set; }
        public static CombatResult  LastResult     { get; private set; }

        // ── Flags de test ─────────────────────────────────────────────────────
        public static bool ForceAutoMode      { get; set; } = false;

        // ── Navegación post-combate ────────────────────────────────────────────
        public static bool NextFaseRequest    { get; set; } = false;
        public static int  NextMundo          { get; set; } = 0;
        public static int  NextFase           { get; set; } = 0;
        public static bool ReturnToPanelFases { get; set; } = false;

        /// Guarda el resultado y preserva LastContext antes de limpiar PendingContext.
        public static void SetResult(CombatResult result)
        {
            LastResult = result;
            if (PendingContext != null) LastContext = PendingContext;
            PendingContext = null;
        }

        /// Limpia el último resultado (llamar después de mostrarlo en CampaignScene).
        public static void ClearLastResult()
        {
            LastResult = null;
        }

        /// Restaura PendingContext desde LastContext con enemigos al HP completo.
        public static void PrepareRepeat()
        {
            if (LastContext == null) return;

            var newCtx = new CombatContext
            {
                encounterID     = LastContext.encounterID,
                callerScene     = LastContext.callerScene,
                combatMode      = LastContext.combatMode,
                maldicionActiva = LastContext.maldicionActiva,
                playerTeam      = LastContext.playerTeam
                    ?.Select(h => new HeroInstance
                    {
                        heroId                = h.heroId,
                        nivel                 = h.nivel,
                        hpActual              = h.hpMax,
                        hpMax                 = h.hpMax,
                        atk                   = h.atk,
                        def                   = h.def,
                        spd                   = h.spd,
                        agi                   = h.agi,
                        crit                  = h.crit,
                        critDmg               = h.critDmg,
                        acc                   = h.acc,
                        res                   = h.res,
                        luk                   = h.luk,
                        elemento              = h.elemento,
                        estaVivo              = true,
                        efectosActivos        = new List<string>(),
                        habilidadesEquipadas  = h.habilidadesEquipadas,
                    }).ToArray(),
                enemyTeam       = LastContext.enemyTeam
                    ?.Select(e => new EnemyInstance
                    {
                        enemyId        = e.enemyId,
                        nombre         = e.nombre,
                        nivel          = e.nivel,
                        hpActual       = e.hpMax,
                        hpMax          = e.hpMax,
                        atk            = e.atk,
                        def            = e.def,
                        spd            = e.spd,
                        agi            = e.agi,
                        elemento       = e.elemento,
                        estaVivo       = true,
                        efectosActivos = new List<string>(),
                    }).ToArray()
            };
            PendingContext = newCtx;
        }
    }
}
