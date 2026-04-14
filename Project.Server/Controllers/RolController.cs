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
    /// Controlador CRUD para la gestión de Roles
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize] // Requiere autenticación JWT
    [ModuleInfo(
        DisplayName = "Roles",
        Description = "Gestión de roles del sistema",
        Icon = "bi-shield-lock",
        Path = "rol",
        Order = 3,
        IsVisible = true
    )]
    public class RolController : CrudController<Rol, RolRequest, RolResponse, long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RolController"/> class.
        /// </summary>
        /// <param name="service">The service<see cref="IEntityService{Rol, RolRequest, long}"/></param>
        /// <param name="mapper">The mapper<see cref="IMapper"/></param>
        public RolController(
            IEntityService<Rol, RolRequest, long> service,
            IMapper mapper) : base(service, mapper)
        {
        }

        /// <summary>
        /// Obtiene todos los roles
        /// GET: api/v1/Rol
        /// </summary>
        [HttpGet]
        [RequireOperation] // Valida permiso basado en Rol.GetAll.GET
        [OperationInfo(
            DisplayName = "Listar Roles",
            Description = "Obtiene la lista de roles con paginación y filtros",
            Icon = "bi-list",
            Path = "rol",
            IsVisible = true
        )]
        public override IActionResult GetAll([FromQuery] QueryParamsRequest query)
        {
            return base.GetAll(query);
        }

        /// <summary>
        /// Obtiene un rol por su Id
        /// GET: api/v1/Rol/{id}
        /// </summary>
        [HttpGet("{id}")]
        [RequireOperation] // Valida permiso basado en Rol.Get.GET
        [OperationInfo(
            DisplayName = "Ver Rol",
            Description = "Obtiene los detalles de un rol específico",
            Icon = "bi-eye",
            Path = "rol/view",
            IsVisible = false
        )]
        public override IActionResult Get(long id, string? include = null)
        {
            return base.Get(id, include);
        }

        /// <summary>
        /// Crea un nuevo rol
        /// POST: api/v1/Rol
        /// </summary>
        [HttpPost]
        [RequireOperation] // Valida permiso basado en Rol.Create.POST
        [OperationInfo(
            DisplayName = "Crear Rol",
            Description = "Crea un nuevo rol en el sistema",
            Icon = "bi-plus-circle",
            Path = "rol/create",
            IsVisible = true
        )]
        public override IActionResult Create([FromBody] RolRequest request)
        {
            return base.Create(request);
        }

        /// <summary>
        /// Actualiza un rol existente
        /// PUT: api/v1/Rol
        /// </summary>
        [HttpPut]
        [RequireOperation] // Valida permiso basado en Rol.Update.PUT
        [OperationInfo(
            DisplayName = "Actualizar Rol",
            Description = "Actualiza completamente un rol existente",
            Icon = "bi-pencil-square",
            Path = "rol/update",
            IsVisible = false
        )]
        public override IActionResult Update([FromBody] RolRequest request)
        {
            return base.Update(request);
        }

        /// <summary>
        /// Actualiza parcialmente un rol
        /// PATCH: api/v1/Rol
        /// </summary>
        [HttpPatch]
        [RequireOperation] // Valida permiso basado en Rol.PartialUpdate.PATCH
        [OperationInfo(
            DisplayName = "Actualizar Parcial Rol",
            Description = "Actualiza parcialmente un rol existente",
            Icon = "bi-pencil",
            Path = "rol/partial-update",
            IsVisible = false
        )]
        public override IActionResult PartialUpdate([FromBody] RolRequest request)
        {
            return base.PartialUpdate(request);
        }

        /// <summary>
        /// Elimina (marca como inactivo) un rol
        /// DELETE: api/v1/Rol/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [RequireOperation] // Valida permiso basado en Rol.Delete.DELETE
        [OperationInfo(
            DisplayName = "Eliminar Rol",
            Description = "Elimina (desactiva) un rol del sistema",
            Icon = "bi-trash",
            Path = "rol/delete",
            IsVisible = false
        )]
        public override IActionResult Delete(long id)
        {
            return base.Delete(id);
        }
    }
}
