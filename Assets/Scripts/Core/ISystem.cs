namespace ReinoOscuridad.Core
{
    /// Contrato que todo sistema del juego debe implementar.
    /// Los sistemas se registran en GameManager y nunca se llaman entre sí directamente;
    /// se comunican exclusivamente a través del EventBus.
    public interface ISystem
    {
        /// Inicialización única al arrancar la aplicación (Awake de GameManager).
        void Initialize();

        /// Llamado al inicio de una sesión de juego (usuario autenticado y datos cargados).
        void OnSessionStart();

        /// Llamado antes de que la app se cierre o la sesión termine.
        /// Aquí se persisten datos si el sistema los gestiona.
        void OnSessionEnd();
    }
}
