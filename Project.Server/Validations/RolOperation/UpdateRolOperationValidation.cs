using FluentValidation;
using Project.Server.Entities.Request;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.RolOperation
{
    /// <summary>
    /// Defines the <see cref="UpdateRolOperationValidation" />
    /// </summary>
    public class UpdateRolOperationValidation : UpdateValidator<RolOperationRequest, long?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateRolOperationValidation"/> class.
        /// </summary>
        public UpdateRolOperationValidation()
        {
            RuleFor(x => x.RolId)
                .NotNull().WithMessage("El rol es requerido")
                .GreaterThan(0).WithMessage("El rol debe ser mayor a 0");

            RuleFor(x => x.OperationId)
                .NotNull().WithMessage("La operación es requerida")
                .GreaterThan(0).WithMessage("La operación debe ser mayor a 0");

            RuleFor(x => x.State)
                .NotNull().WithMessage("El estado es requerido");
        }
    }
}
