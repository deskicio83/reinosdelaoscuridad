namespace ReinoOscuridad.Data
{
    // ── Enumeraciones del proyecto ─────────────────────────────────────────────
    // Centralizar aquí todos los enums para evitar duplicados entre namespaces.

    /// Tipo de moneda/recurso visible en el HUD.
    public enum CurrencyType
    {
        Energy,
        Gold,
        Caosifera
    }

    /// Efectos de estado que se pueden aplicar en combate.
    /// Bleed NO puede ser removido por Cleanse.
    /// Stun salta el turno del objetivo.
    /// Shield absorbe daño antes de restar HP.
    public enum TipoEfecto
    {
        Bleed,
        Burn,
        Poison,
        Stun,
        Freeze,
        Sleep,
        Silence,
        Shield,
        Barrier,
        Regen,
        Haste,
        Slow,
        Provoke,
        Cleanse,
        Immunity,
        Reflect,
        Lifesteal
    }
}
