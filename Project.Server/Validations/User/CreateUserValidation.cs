using FluentValidation;
using Project.Server.Entities.Request;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.User
{
    /// <summary>
    /// Validador para la creación de usuarios
    /// </summary>
    public class CreateUserValidation : CreateValidator<UserRequest, long?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateUserValidation"/> class.
        /// </summary>
        public CreateUserValidation()
        {
            RuleFor(x => x.RolId)
                .NotNull().WithMessage("El Rol es requerido")
                .NotEmpty().WithMessage("El Rol no puede ser vacío")
                .GreaterThan(0).WithMessage("El Rol debe ser válido");

            RuleFor(x => x.Email)
                .NotNull().WithMessage("El Email es requerido")
                .NotEmpty().WithMessage("El Email no puede ser vacío")
                .EmailAddress().WithMessage("El Email no es válido")
                .MaximumLength(100).WithMessage("El Email no puede exceder 100 caracteres");

            RuleFor(x => x.Name)
                .NotNull().WithMessage("El Nombre es requerido")
                .NotEmpty().WithMessage("El Nombre no puede ser vacío")
                .MaximumLength(150).WithMessage("El Nombre no puede exceder 150 caracteres");

            RuleFor(x => x.UserName)
                .NotNull().WithMessage("El Nombre de Usuario es requerido")
                .NotEmpty().WithMessage("El Nombre de Usuario no puede ser vacío")
                .MinimumLength(4).WithMessage("El Nombre de Usuario debe tener al menos 4 caracteres")
                .MaximumLength(50).WithMessage("El Nombre de Usuario no puede exceder 50 caracteres");

            RuleFor(x => x.Password)
                .NotNull().WithMessage("La Contraseña es requerida")
                .NotEmpty().WithMessage("La Contraseña no puede ser vacía")
                .MinimumLength(6).WithMessage("La Contraseña debe tener al menos 6 caracteres")
                .MaximumLength(100).WithMessage("La Contraseña no puede exceder 100 caracteres");

            RuleFor(x => x.IdentificationDocument)
                .MaximumLength(50).WithMessage("El Documento de Identificación no puede exceder 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.IdentificationDocument));

            RuleFor(x => x.Number)
                .MaximumLength(20).WithMessage("El Número no puede exceder 20 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Number));

            RuleFor(x => x.State)
                .NotNull().WithMessage("El Estado es requerido")
                .InclusiveBetween(0, 1).WithMessage("El Estado debe ser 0 (Inactivo) o 1 (Activo)");
        }
    }
}
