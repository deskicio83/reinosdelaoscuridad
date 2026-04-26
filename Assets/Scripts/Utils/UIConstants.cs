namespace ReinoOscuridad.Utils
{
    /// Constantes de layout UI compartidas por todos los Editor Scripts
    /// y por cualquier código runtime que necesite los márgenes globales.
    public static class UIConstants
    {
        // El HUD ocupa el 10% superior — todas las Scenes respetan este margen.
        public const float HUD_HEIGHT_PERCENT    = 0.10f;
        public const float CONTENT_START_PERCENT = 0.90f; // anchorMax.y del contenido
        public const float CONTENT_END_PERCENT   = 0.00f; // anchorMin.y del contenido

        // Versión del formato de PlayerData. Incrementar cuando cambie incompatiblemente.
        public const int    DATA_VERSION     = 1;
        public const string DATA_VERSION_KEY = "reino_data_version";
    }
}
