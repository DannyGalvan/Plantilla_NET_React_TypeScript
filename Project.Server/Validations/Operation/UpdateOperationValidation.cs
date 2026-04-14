using FluentValidation;
using Project.Server.Entities.Request;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.Operation
{
    /// <summary>
    /// Defines the <see cref="UpdateOperationValidation" />
    /// </summary>
    public class UpdateOperationValidation : UpdateValidator<OperationRequest, long?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateOperationValidation"/> class.
        /// </summary>
        public UpdateOperationValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("La descripción es requerida")
                .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

            RuleFor(x => x.Policy)
                .NotEmpty().WithMessage("La política es requerida")
                .MaximumLength(100).WithMessage("La política no puede exceder 100 caracteres");

            RuleFor(x => x.ModuleId)
                .NotNull().WithMessage("El módulo es requerido")
                .GreaterThan(0).WithMessage("El módulo debe ser mayor a 0");

            RuleFor(x => x.ControllerName)
                .NotEmpty().WithMessage("El nombre del controlador es requerido")
                .MaximumLength(100).WithMessage("El nombre del controlador no puede exceder 100 caracteres");

            RuleFor(x => x.ActionName)
                .NotEmpty().WithMessage("El nombre de la acción es requerido")
                .MaximumLength(100).WithMessage("El nombre de la acción no puede exceder 100 caracteres");

            RuleFor(x => x.HttpMethod)
                .NotEmpty().WithMessage("El método HTTP es requerido")
                .Must(x => new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }.Contains(x))
                .WithMessage("El método HTTP debe ser GET, POST, PUT, DELETE o PATCH");

            RuleFor(x => x.OperationKey)
                .NotEmpty().WithMessage("La clave de operación es requerida")
                .MaximumLength(200).WithMessage("La clave de operación no puede exceder 200 caracteres");

            RuleFor(x => x.State)
                .NotNull().WithMessage("El estado es requerido");
        }
    }
}
