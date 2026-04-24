namespace ReinoOscuridad.Data
{
    /// Pasarela estática de datos entre Scenes de combate.
    /// Antes de navegar a CombatScene: asignar PendingContext.
    /// En CombatSceneController.Start(): leer PendingContext.
    /// Al terminar: SetResult() guarda el resultado y limpia PendingContext.
    public static class CombatSceneData
    {
        public static CombatContext PendingContext { get; set; }
        public static CombatResult  LastResult     { get; private set; }

        // ── Navegación post-combate (FIX 6) ───────────────────────────────────
        /// Si true, CampaignScene abre PanelBatalla de NextMundo/NextFase al volver.
        public static bool NextFaseRequest    { get; set; } = false;
        public static int  NextMundo          { get; set; } = 0;
        public static int  NextFase           { get; set; } = 0;
        /// Si true, CampaignScene abre PanelFases (sin PanelBatalla) del NextMundo.
        public static bool ReturnToPanelFases { get; set; } = false;

        /// Guarda el resultado del combate y limpia el contexto pendiente.
        public static void SetResult(CombatResult result)
        {
            LastResult     = result;
            PendingContext = null;
        }
    }
}
