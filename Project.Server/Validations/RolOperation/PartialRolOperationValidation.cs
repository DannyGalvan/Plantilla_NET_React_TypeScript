using FluentValidation;
using Project.Server.Entities.Request;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.RolOperation
{
    /// <summary>
    /// Defines the <see cref="PartialRolOperationValidation" />
    /// </summary>
    public class PartialRolOperationValidation : PartialUpdateValidator<RolOperationRequest, long?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialRolOperationValidation"/> class.
        /// </summary>
        public PartialRolOperationValidation()
        {
            When(x => x.RolId != null, () =>
            {
                RuleFor(x => x.RolId)
                    .GreaterThan(0).WithMessage("El rol debe ser mayor a 0");
            });

            When(x => x.OperationId != null, () =>
            {
                RuleFor(x => x.OperationId)
                    .GreaterThan(0).WithMessage("La operación debe ser mayor a 0");
            });
        }
    }
}
