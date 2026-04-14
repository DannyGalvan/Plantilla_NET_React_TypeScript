namespace Project.Server.Attributes
{
    /// <summary>
    /// Atributo para definir metadata de un módulo (Controlador)
    /// Usado por el sistema de sincronización automática de permisos
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModuleInfoAttribute : Attribute
    {
        /// <summary>
        /// Nombre visible del módulo para el frontend
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del módulo
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Icono del módulo (para UI)
        /// </summary>
        public string Icon { get; set; } = "folder";

        /// <summary>
        /// Ruta del módulo en el frontend
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Orden de visualización del módulo
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Indica si el módulo es visible en el menú
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }
}
