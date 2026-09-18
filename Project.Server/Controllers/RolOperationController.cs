using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Attributes;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Security.Authorization;
using Project.Server.Services.Interfaces;

namespace Project.Server.Controllers
{
    /// <summary>
    /// Controlador CRUD para la gestión de Operaciones de Rol
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize] // Requiere autenticación JWT
    [ModuleInfo(
        DisplayName = "Rol Operations",
        Description = "Gestión de operaciones asignadas a roles",
        Icon = "bi-shield-lock-fill",
        Path = "roloperation",
        Order = 5,
        IsVisible = false
    )]
    public class RolOperationController : CrudController<RolOperation, RolOperationRequest, RolOperationResponse, long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RolOperationController"/> class.
        /// </summary>
        /// <param name="service">The service<see cref="IEntityService{RolOperation, RolOperationRequest, long}"/></param>
        /// <param name="mapper">The mapper<see cref="IMapper"/></param>
        public RolOperationController(
            IEntityService<RolOperation, RolOperationRequest, long> service,
            IMapper mapper) : base(service, mapper)
        {
        }

        /// <summary>
        /// Obtiene todas las operaciones de rol
        /// GET: api/v1/RolOperation
        /// </summary>
        [HttpGet]
        [RequireOperation] // Valida permiso basado en RolOperation.GetAll.GET
        [OperationInfo(
            DisplayName = "Listar Operaciones de Rol",
            Description = "Obtiene la lista de operaciones asignadas a roles con paginación y filtros",
            Icon = "bi-list",
            Path = "roloperation",
            IsVisible = false
        )]
        public override async Task<IActionResult> GetAll([FromQuery] QueryParamsRequest query, CancellationToken ct = default)
        {
            return await base.GetAll(query, ct);
        }

        /// <summary>
        /// Obtiene una operación de rol por su Id
        /// GET: api/v1/RolOperation/{id}
        /// </summary>
        [HttpGet("{id}")]
        [RequireOperation] // Valida permiso basado en RolOperation.Get.GET
        [OperationInfo(
            DisplayName = "Ver Operación de Rol",
            Description = "Obtiene los detalles de una operación de rol específica",
            Icon = "bi-eye",
            Path = "roloperation/view",
            IsVisible = false
        )]
        public override async Task<IActionResult> Get(long id, string? include = null, CancellationToken ct = default)
        {
            return await base.Get(id, include, ct);
        }

        /// <summary>
        /// Crea una nueva operación de rol
        /// POST: api/v1/RolOperation
        /// </summary>
        [HttpPost]
        [RequireOperation] // Valida permiso basado en RolOperation.Create.POST
        [OperationInfo(
            DisplayName = "Crear Operación de Rol",
            Description = "Asigna una nueva operación a un rol",
            Icon = "bi-plus-circle",
            Path = "roloperation/create",
            IsVisible = false
        )]
        public override async Task<IActionResult> Create([FromBody] RolOperationRequest request, CancellationToken ct = default)
        {
            return await base.Create(request, ct);
        }

        /// <summary>
        /// Actualiza una operación de rol existente (actualización completa)
        /// PUT: api/v1/RolOperation
        /// </summary>
        [HttpPut]
        [RequireOperation] // Valida permiso basado en RolOperation.Update.PUT
        [OperationInfo(
            DisplayName = "Actualizar Operación de Rol",
            Description = "Actualiza todos los campos de una operación de rol existente",
            Icon = "bi-pencil-square",
            Path = "roloperation/edit",
            IsVisible = false
        )]
        public override async Task<IActionResult> Update([FromBody] RolOperationRequest request, CancellationToken ct = default)
        {
            return await base.Update(request, ct);
        }

        /// <summary>
        /// Actualiza parcialmente una operación de rol existente
        /// PATCH: api/v1/RolOperation
        /// </summary>
        [HttpPatch]
        [RequireOperation] // Valida permiso basado en RolOperation.PartialUpdate.PATCH
        [OperationInfo(
            DisplayName = "Actualizar Parcialmente Operación de Rol",
            Description = "Actualiza campos específicos de una operación de rol existente",
            Icon = "bi-pencil",
            Path = "roloperation/partial-edit",
            IsVisible = false
        )]
        public override async Task<IActionResult> PartialUpdate([FromBody] RolOperationRequest request, CancellationToken ct = default)
        {
            return await base.PartialUpdate(request, ct);
        }

        /// <summary>
        /// Elimina una operación de rol
        /// DELETE: api/v1/RolOperation/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [RequireOperation] // Valida permiso basado en RolOperation.Delete.DELETE
        [OperationInfo(
            DisplayName = "Eliminar Operación de Rol",
            Description = "Elimina una operación asignada a un rol",
            Icon = "bi-trash",
            Path = "roloperation/delete",
            IsVisible = false
        )]
        public override async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        {
            return await base.Delete(id, ct);
        }
    }
}
