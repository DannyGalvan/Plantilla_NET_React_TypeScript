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
    /// Controlador CRUD para la gestión de Operaciones
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [ModuleInfo(
        DisplayName = "Operations",
        Description = "Gestión de operaciones del sistema",
        Icon = "bi-gear-fill",
        Path = "operation",
        Order = 4,
        IsVisible = false
    )]
    public class OperationController : CrudController<Operation, OperationRequest, OperationResponse, long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationController"/> class.
        /// </summary>
        /// <param name="service">The service<see cref="IEntityService{Operation, OperationRequest, long}"/></param>
        /// <param name="mapper">The mapper<see cref="IMapper"/></param>
        public OperationController(
            IEntityService<Operation, OperationRequest, long> service,
            IMapper mapper) : base(service, mapper)
        {
        }

        /// <summary>
        /// Obtiene todas las operaciones
        /// GET: api/v1/Operation
        /// </summary>
        [HttpGet]
        [OperationInfo(
            DisplayName = "Listar Operaciones",
            Description = "Obtiene la lista de operaciones con paginación y filtros",
            Icon = "bi-list",
            Path = "operation",
            IsVisible = false
        )]
        public override IActionResult GetAll([FromQuery] QueryParamsRequest query)
        {
            return base.GetAll(query);
        }

        /// <summary>
        /// Obtiene una operación por su Id
        /// GET: api/v1/Operation/{id}
        /// </summary>
        [HttpGet("{id}")]
        [OperationInfo(
            DisplayName = "Ver Operación",
            Description = "Obtiene los detalles de una operación específica",
            Icon = "bi-eye",
            Path = "operation/view",
            IsVisible = false
        )]
        public override IActionResult Get(long id, string? include = null)
        {
            return base.Get(id, include);
        }

        /// <summary>
        /// Crea una nueva operación
        /// POST: api/v1/Operation
        /// </summary>
        [HttpPost]
        [OperationInfo(
            DisplayName = "Crear Operación",
            Description = "Crea una nueva operación en el sistema",
            Icon = "bi-plus-circle",
            Path = "operation/create",
            IsVisible = false
        )]
        public override IActionResult Create([FromBody] OperationRequest request)
        {
            return base.Create(request);
        }

        /// <summary>
        /// Actualiza una operación existente (actualización completa)
        /// PUT: api/v1/Operation
        /// </summary>
        [HttpPut]
        [OperationInfo(
            DisplayName = "Actualizar Operación",
            Description = "Actualiza todos los campos de una operación existente",
            Icon = "bi-pencil-square",
            Path = "operation/edit",
            IsVisible = false
        )]
        public override IActionResult Update([FromBody] OperationRequest request)
        {
            return base.Update(request);
        }

        /// <summary>
        /// Actualiza parcialmente una operación existente
        /// PATCH: api/v1/Operation
        /// </summary>
        [HttpPatch]
        [OperationInfo(
            DisplayName = "Actualizar Parcialmente Operación",
            Description = "Actualiza campos específicos de una operación existente",
            Icon = "bi-pencil",
            Path = "operation/partial-edit",
            IsVisible = false
        )]
        public override IActionResult PartialUpdate([FromBody] OperationRequest request)
        {
            return base.PartialUpdate(request);
        }

        /// <summary>
        /// Elimina una operación
        /// DELETE: api/v1/Operation/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [OperationInfo(
            DisplayName = "Eliminar Operación",
            Description = "Elimina una operación del sistema",
            Icon = "bi-trash",
            Path = "operation/delete",
            IsVisible = false
        )]
        public override IActionResult Delete(long id)
        {
            return base.Delete(id);
        }
    }
}
