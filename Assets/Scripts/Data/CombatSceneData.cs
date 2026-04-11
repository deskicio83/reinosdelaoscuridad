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

        /// Guarda el resultado del combate y limpia el contexto pendiente.
        public static void SetResult(CombatResult result)
        {
            LastResult     = result;
            PendingContext = null;
        }
    }
}
