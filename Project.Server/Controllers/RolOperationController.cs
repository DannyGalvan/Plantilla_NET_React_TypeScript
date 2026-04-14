using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Attributes;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Services.Interfaces;

namespace Project.Server.Controllers
{
    /// <summary>
    /// Controlador CRUD para la gestión de Operaciones de Rol
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
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
        [OperationInfo(
            DisplayName = "Listar Operaciones de Rol",
            Description = "Obtiene la lista de operaciones asignadas a roles con paginación y filtros",
            Icon = "bi-list",
            Path = "roloperation",
            IsVisible = false
        )]
        public override IActionResult GetAll([FromQuery] QueryParamsRequest query)
        {
            return base.GetAll(query);
        }

        /// <summary>
        /// Obtiene una operación de rol por su Id
        /// GET: api/v1/RolOperation/{id}
        /// </summary>
        [HttpGet("{id}")]
        [OperationInfo(
            DisplayName = "Ver Operación de Rol",
            Description = "Obtiene los detalles de una operación de rol específica",
            Icon = "bi-eye",
            Path = "roloperation/view",
            IsVisible = false
        )]
        public override IActionResult Get(long id, string? include = null)
        {
            return base.Get(id, include);
        }

        /// <summary>
        /// Crea una nueva operación de rol
        /// POST: api/v1/RolOperation
        /// </summary>
        [HttpPost]
        [OperationInfo(
            DisplayName = "Crear Operación de Rol",
            Description = "Asigna una nueva operación a un rol",
            Icon = "bi-plus-circle",
            Path = "roloperation/create",
            IsVisible = false
        )]
        public override IActionResult Create([FromBody] RolOperationRequest request)
        {
            return base.Create(request);
        }

        /// <summary>
        /// Actualiza una operación de rol existente (actualización completa)
        /// PUT: api/v1/RolOperation
        /// </summary>
        [HttpPut]
        [OperationInfo(
            DisplayName = "Actualizar Operación de Rol",
            Description = "Actualiza todos los campos de una operación de rol existente",
            Icon = "bi-pencil-square",
            Path = "roloperation/edit",
            IsVisible = false
        )]
        public override IActionResult Update([FromBody] RolOperationRequest request)
        {
            return base.Update(request);
        }

        /// <summary>
        /// Actualiza parcialmente una operación de rol existente
        /// PATCH: api/v1/RolOperation
        /// </summary>
        [HttpPatch]
        [OperationInfo(
            DisplayName = "Actualizar Parcialmente Operación de Rol",
            Description = "Actualiza campos específicos de una operación de rol existente",
            Icon = "bi-pencil",
            Path = "roloperation/partial-edit",
            IsVisible = false
        )]
        public override IActionResult PartialUpdate([FromBody] RolOperationRequest request)
        {
            return base.PartialUpdate(request);
        }

        /// <summary>
        /// Elimina una operación de rol
        /// DELETE: api/v1/RolOperation/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [OperationInfo(
            DisplayName = "Eliminar Operación de Rol",
            Description = "Elimina una operación asignada a un rol",
            Icon = "bi-trash",
            Path = "roloperation/delete",
            IsVisible = false
        )]
        public override IActionResult Delete(long id)
        {
            return base.Delete(id);
        }
    }
}
