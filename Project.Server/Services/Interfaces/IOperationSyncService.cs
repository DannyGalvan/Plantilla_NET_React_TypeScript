namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Servicio para sincronizar automáticamente Módulos y Operaciones desde los controladores
    /// usando reflexión y atributos personalizados
    /// </summary>
    public interface IOperationSyncService
    {
        /// <summary>
        /// Sincroniza todos los módulos y operaciones detectados en los controladores con la base de datos
        /// </summary>
        /// <returns>Task completado</returns>
        Task SyncAsync();

        /// <summary>
        /// Asigna todas las operaciones activas al rol especificado (por defecto SA - Super Administrador)
        /// </summary>
        /// <param name="roleName">Nombre del rol al que se asignarán las operaciones</param>
        /// <returns>Task completado</returns>
        Task AssignAllOperationsToAdminRoleAsync(string roleName = "SA");
    }
}
