using FluentValidation;
using Project.Server.Entities.Request;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.User
{
    /// <summary>
    /// Defines the <see cref="PartialUserValidation" />
    /// </summary>
    public class PartialUserValidation : PartialUpdateValidator<UserRequest, long?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialUserValidation"/> class.
        /// </summary>
        public PartialUserValidation()
        {
            // Solo validar cuando el campo está presente y no es null
            When(x => x.RolId != null, () =>
            {
                RuleFor(x => x.RolId)
                    .NotEmpty().WithMessage("El rol es obligatorio")
                    .GreaterThan(0).WithMessage("El rol debe ser un valor válido");
            });

            When(x => !string.IsNullOrEmpty(x.Email), () =>
            {
                RuleFor(x => x.Email)
                    .EmailAddress().WithMessage("El email debe ser válido")
                    .MaximumLength(100).WithMessage("El email no debe exceder los 100 caracteres");
            });

            When(x => !string.IsNullOrEmpty(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .MaximumLength(150).WithMessage("El nombre no debe exceder los 150 caracteres");
            });

            When(x => !string.IsNullOrEmpty(x.UserName), () =>
            {
                RuleFor(x => x.UserName)
                    .MinimumLength(4).WithMessage("El nombre de usuario debe tener al menos 4 caracteres")
                    .MaximumLength(50).WithMessage("El nombre de usuario no debe exceder los 50 caracteres");
            });

            When(x => !string.IsNullOrEmpty(x.Password), () =>
            {
                RuleFor(x => x.Password)
                    .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres")
                    .MaximumLength(100).WithMessage("La contraseña no debe exceder los 100 caracteres");
            });

            When(x => x.State != null, () =>
            {
                RuleFor(x => x.State)
                    .InclusiveBetween(0, 1).WithMessage("El estado debe ser 0 (Inactivo) o 1 (Activo)");
            });
        }
    }
}
