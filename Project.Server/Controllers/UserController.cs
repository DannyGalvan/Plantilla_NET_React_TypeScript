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
    /// Controlador CRUD para la gestión de Usuarios
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [ModuleInfo(
        DisplayName = "Usuarios",
        Description = "Gestión de usuarios del sistema",
        Icon = "bi-people-fill",
        Path = "user",
        Order = 2,
        IsVisible = true
    )]
    public class UserController : CrudController<User, UserRequest, UserResponse, long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="service">The service<see cref="IEntityService{User, UserRequest, long}"/></param>
        /// <param name="mapper">The mapper<see cref="IMapper"/></param>
        public UserController(
            IEntityService<User, UserRequest, long> service,
            IMapper mapper) : base(service, mapper)
        {
        }

        /// <summary>
        /// Obtiene todos los usuarios
        /// GET: api/v1/User
        /// </summary>
        [HttpGet]
        [RequireOperation]
        [OperationInfo(
            DisplayName = "Listar Usuarios",
            Description = "Obtiene la lista de usuarios con paginación y filtros",
            Icon = "bi-list",
            Path = "user",
            IsVisible = true
        )]
        public override IActionResult GetAll([FromQuery] QueryParamsRequest query)
        {
            return base.GetAll(query);
        }

        /// <summary>
        /// Obtiene un usuario por su Id
        /// GET: api/v1/User/{id}
        /// </summary>
        [HttpGet("{id}")]
        [RequireOperation]
        [OperationInfo(
            DisplayName = "Ver Usuario",
            Description = "Obtiene los detalles de un usuario específico",
            Icon = "bi-eye",
            Path = "user/view",
            IsVisible = false
        )]
        public override IActionResult Get(long id, string? include = null)
        {
            return base.Get(id, include);
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// POST: api/v1/User
        /// </summary>
        [HttpPost]
        [RequireOperation]
        [OperationInfo(
            DisplayName = "Crear Usuario",
            Description = "Crea un nuevo usuario en el sistema",
            Icon = "bi-plus-circle",
            Path = "user/create",
            IsVisible = true
        )]
        public override IActionResult Create([FromBody] UserRequest request)
        {
            return base.Create(request);
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// PUT: api/v1/User
        /// </summary>
        [HttpPut]
        [RequireOperation]
        [OperationInfo(
            DisplayName = "Actualizar Usuario",
            Description = "Actualiza completamente un usuario existente",
            Icon = "bi-pencil",
            Path = "user/update",
            IsVisible = false
        )]
        public override IActionResult Update([FromBody] UserRequest request)
        {
            return base.Update(request);
        }

        /// <summary>
        /// Actualiza parcialmente un usuario
        /// PATCH: api/v1/User
        /// </summary>
        [HttpPatch]
        [RequireOperation]
        [OperationInfo(
            DisplayName = "Actualización Parcial Usuario",
            Description = "Actualiza parcialmente un usuario existente",
            Icon = "bi-pencil-square",
            Path = "user/partial-update",
            IsVisible = false
        )]
        public override IActionResult PartialUpdate([FromBody] UserRequest request)
        {
            return base.PartialUpdate(request);
        }

        /// <summary>
        /// Elimina (lógicamente) un usuario
        /// DELETE: api/v1/User/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [RequireOperation] // Valida permiso basado en User.Delete.DELETE
        [OperationInfo(
            DisplayName = "Eliminar Usuario",
            Description = "Elimina lógicamente un usuario del sistema",
            Icon = "bi-trash",
            Path = "user/delete",
            IsVisible = false
        )]
        public override IActionResult Delete(long id)
        {
            return base.Delete(id);
        }
    }
}
