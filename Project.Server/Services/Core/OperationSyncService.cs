using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Project.Server.Attributes;
using Project.Server.Context;
using Project.Server.Entities.Models;
using Project.Server.Services.Interfaces;

namespace Project.Server.Services.Core
{
    /// <summary>
    /// Servicio de sincronización automática de Módulos y Operaciones
    /// Usa reflexión para detectar controladores y acciones, y los sincroniza con la BD
    /// </summary>
    public class OperationSyncService : IOperationSyncService
    {
        private readonly DataContext _db;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;
        private readonly ILogger<OperationSyncService> _logger;

        public OperationSyncService(
            DataContext db,
            IActionDescriptorCollectionProvider actionDescriptorProvider,
            ILogger<OperationSyncService> logger)
        {
            _db = db;
            _actionDescriptorProvider = actionDescriptorProvider;
            _logger = logger;
        }

        public async Task SyncAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando sincronización de operaciones...");

                // Obtener todas las acciones de los controladores
                var actions = _actionDescriptorProvider.ActionDescriptors.Items
                    .OfType<ControllerActionDescriptor>()
                    .Where(a => !IsExcludedController(a) && !IsExcludedAction(a))
                    .ToList();

                _logger.LogInformation("Total de acciones detectadas: {Count}", actions.Count);

                // Normalizar acciones y agrupar por clave única
                var normalizedActions = actions
                    .Select(a =>
                    {
                        var methods = a.ActionConstraints?
                            .OfType<HttpMethodActionConstraint>()
                            .FirstOrDefault()?
                            .HttpMethods
                            ?.Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList() ?? new List<string>();

                        var method = methods.Count > 0 ? string.Join("_", methods).ToUpperInvariant() : "ANY";
                        var key = $"{a.ControllerName}.{a.ActionName}.{method}";

                        return new
                        {
                            Key = key,
                            ControllerName = a.ControllerName,
                            ActionName = a.ActionName,
                            HttpMethod = method,
                            RouteTemplate = a.AttributeRouteInfo?.Template ?? string.Empty,
                            ControllerType = a.ControllerTypeInfo,
                            MethodInfo = a.MethodInfo
                        };
                    })
                    .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                _logger.LogInformation("Acciones normalizadas: {Count}", normalizedActions.Count);

                // Cargar módulos y operaciones existentes
                var modules = await _db.Modules.ToListAsync();
                var operations = await _db.Operations.ToListAsync();

                // Procesar cada acción
                foreach (var action in normalizedActions)
                {
                    // Obtener o crear el módulo
                    var module = modules.FirstOrDefault(m => m.Name == action.ControllerName);
                    if (module == null)
                    {
                        var moduleInfo = action.ControllerType.GetCustomAttributes(typeof(ModuleInfoAttribute), false)
                            .FirstOrDefault() as ModuleInfoAttribute;

                        module = new Module
                        {
                            Name = action.ControllerName,
                            Description = moduleInfo?.Description ?? $"Módulo {action.ControllerName}",
                            Image = moduleInfo?.Icon ?? "folder",
                            Path = moduleInfo?.Path ?? action.ControllerName,
                            State = 1,
                            Order = moduleInfo?.Order ?? 0,
                            CreatedBy = 1,
                            CreatedAt = DateTime.UtcNow
                        };

                        _db.Modules.Add(module);
                        modules.Add(module);
                        await _db.SaveChangesAsync();

                        _logger.LogInformation("Módulo creado: {ModuleName}", module.Name);
                    }
                    else
                    {
                        // Actualizar módulo existente con metadata de atributo
                        var moduleInfo = action.ControllerType.GetCustomAttributes(typeof(ModuleInfoAttribute), false)
                            .FirstOrDefault() as ModuleInfoAttribute;

                        if (moduleInfo != null)
                        {
                            module.Description = moduleInfo.Description;
                            module.Image = moduleInfo.Icon;
                            module.Path = moduleInfo.Path;
                            module.Order = moduleInfo.Order;
                            module.UpdatedBy = 1;
                            module.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    // Obtener metadata de la operación
                    var operationInfo = action.MethodInfo.GetCustomAttributes(typeof(OperationInfoAttribute), false)
                        .FirstOrDefault() as OperationInfoAttribute;

                    // Generar datos de la operación
                    var key = action.Key;
                    var displayName = operationInfo?.DisplayName ?? $"{action.ControllerName} - {action.ActionName}";
                    var description = operationInfo?.Description ?? $"{action.ActionName} en {action.ControllerName}";
                    var icon = operationInfo?.Icon ?? "circle";
                    var path = operationInfo?.Path ?? $"{action.ControllerName}/{action.ActionName}";
                    var isVisible = operationInfo?.IsVisible ?? false;
                    var policy = $"{action.ControllerName}.{action.ActionName}";

                    // Buscar operación existente
                    var existingOperation = operations.FirstOrDefault(o => o.OperationKey == key);

                    if (existingOperation == null)
                    {
                        // Crear nueva operación
                        var operation = new Operation
                        {
                            ModuleId = module.Id,
                            OperationKey = key,
                            Guid = System.Guid.NewGuid().ToString(),
                            Name = displayName,
                            Description = description,
                            Policy = policy,
                            Icon = icon,
                            Path = path,
                            ControllerName = action.ControllerName,
                            ActionName = action.ActionName,
                            HttpMethod = action.HttpMethod,
                            RouteTemplate = action.RouteTemplate,
                            IsVisible = isVisible,
                            State = 1,
                            CreatedBy = 1,
                            CreatedAt = DateTime.UtcNow
                        };

                        _db.Operations.Add(operation);
                        operations.Add(operation);

                        _logger.LogInformation("Operación creada: {OperationKey}", key);
                    }
                    else
                    {
                        // Actualizar operación existente
                        existingOperation.ModuleId = module.Id;
                        existingOperation.Name = displayName;
                        existingOperation.Description = description;
                        existingOperation.Policy = policy;
                        existingOperation.Icon = icon;
                        existingOperation.Path = path;
                        existingOperation.ControllerName = action.ControllerName;
                        existingOperation.ActionName = action.ActionName;
                        existingOperation.HttpMethod = action.HttpMethod;
                        existingOperation.RouteTemplate = action.RouteTemplate;
                        existingOperation.IsVisible = isVisible;
                        existingOperation.State = 1;
                        existingOperation.UpdatedBy = 1;
                        existingOperation.UpdatedAt = DateTime.UtcNow;

                        _logger.LogDebug("Operación actualizada: {OperationKey}", key);
                    }
                }

                await _db.SaveChangesAsync();

                // Asignar todas las operaciones al rol SA
                await AssignAllOperationsToAdminRoleAsync();

                _logger.LogInformation("Sincronización completada. Total de operaciones: {Count}", await _db.Operations.CountAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización de operaciones");
                throw;
            }
        }

        public async Task AssignAllOperationsToAdminRoleAsync(string roleName = "SA")
        {
            try
            {
                _logger.LogInformation("Asignando operaciones al rol: {RoleName}", roleName);

                // Buscar el rol
                var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                {
                    _logger.LogWarning("Rol {RoleName} no encontrado. No se asignarán operaciones.", roleName);
                    return;
                }

                // Obtener todas las operaciones activas
                var operations = await _db.Operations.Where(o => o.State == 1).ToListAsync();

                // Obtener las asignaciones existentes
                var existingAssignments = await _db.RolOperations
                    .Where(ro => ro.RolId == role.Id)
                    .ToListAsync();

                var existingOperationIds = existingAssignments.Select(ro => ro.OperationId).ToHashSet();

                // Crear asignaciones faltantes
                var newAssignments = operations
                    .Where(o => !existingOperationIds.Contains(o.Id))
                    .Select(o => new RolOperation
                    {
                        RolId = role.Id,
                        OperationId = o.Id,
                        State = 1,
                        CreatedBy = 1,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (newAssignments.Any())
                {
                    _db.RolOperations.AddRange(newAssignments);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Se asignaron {Count} nuevas operaciones al rol {RoleName}", newAssignments.Count, roleName);
                }
                else
                {
                    _logger.LogInformation("No hay nuevas operaciones para asignar al rol {RoleName}", roleName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar operaciones al rol {RoleName}", roleName);
                throw;
            }
        }

        /// <summary>
        /// Verifica si el controlador está excluido de la sincronización
        /// </summary>
        private bool IsExcludedController(ControllerActionDescriptor descriptor)
        {
            return descriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(ExcludeFromSyncAttribute), false)
                .Any();
        }

        /// <summary>
        /// Verifica si la acción está excluida de la sincronización
        /// </summary>
        private bool IsExcludedAction(ControllerActionDescriptor descriptor)
        {
            return descriptor.MethodInfo
                .GetCustomAttributes(typeof(ExcludeFromSyncAttribute), false)
                .Any() ||
                descriptor.MethodInfo
                .GetCustomAttributes(typeof(OperationInfoAttribute), false)
                .OfType<OperationInfoAttribute>()
                .Any(attr => attr.ExcludeFromSync);
        }
    }
}
