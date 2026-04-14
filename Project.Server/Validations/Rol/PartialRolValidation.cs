using FluentValidation;
using Project.Server.Entities.Request;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.Rol
{
    /// <summary>
    /// Defines the <see cref="PartialRolValidation" />
    /// </summary>
    public class PartialRolValidation : PartialUpdateValidator<RolRequest, long?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialRolValidation"/> class.
        /// </summary>
        public PartialRolValidation()
        {
            // Solo validar cuando el campo está presente y no es null
            When(x => !string.IsNullOrEmpty(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .MaximumLength(100).WithMessage("El nombre no debe exceder los 100 caracteres");
            });

            When(x => !string.IsNullOrEmpty(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(250).WithMessage("La descripción no debe exceder los 250 caracteres");
            });

            When(x => x.State != null, () =>
            {
                RuleFor(x => x.State)
                    .InclusiveBetween(0, 1).WithMessage("El estado debe ser 0 (Inactivo) o 1 (Activo)");
            });
        }
    }
}
