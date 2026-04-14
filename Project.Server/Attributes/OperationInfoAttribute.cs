namespace Project.Server.Attributes
{
    /// <summary>
    /// Atributo para definir metadata de una operación (Acción de controlador)
    /// Usado por el sistema de sincronización automática de permisos
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OperationInfoAttribute : Attribute
    {
        /// <summary>
        /// Nombre visible de la operación para el frontend
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Descripción de la operación
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Icono de la operación (para UI)
        /// </summary>
        public string Icon { get; set; } = "circle";

        /// <summary>
        /// Ruta de la operación en el frontend
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Indica si la operación es visible en el menú
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Si es true, esta operación no se sincronizará con la BD
        /// </summary>
        public bool ExcludeFromSync { get; set; } = false;
    }
}
