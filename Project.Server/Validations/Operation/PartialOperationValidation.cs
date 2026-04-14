using FluentValidation;
using Project.Server.Entities.Request;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.Operation
{
    /// <summary>
    /// Defines the <see cref="PartialOperationValidation" />
    /// </summary>
    public class PartialOperationValidation : PartialUpdateValidator<OperationRequest, long?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialOperationValidation"/> class.
        /// </summary>
        public PartialOperationValidation()
        {
            When(x => x.Name != null, () =>
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("El nombre no puede estar vacío")
                    .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");
            });

            When(x => x.Description != null, () =>
            {
                RuleFor(x => x.Description)
                    .NotEmpty().WithMessage("La descripción no puede estar vacía")
                    .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");
            });

            When(x => x.Policy != null, () =>
            {
                RuleFor(x => x.Policy)
                    .NotEmpty().WithMessage("La política no puede estar vacía")
                    .MaximumLength(100).WithMessage("La política no puede exceder 100 caracteres");
            });

            When(x => x.ModuleId != null, () =>
            {
                RuleFor(x => x.ModuleId)
                    .GreaterThan(0).WithMessage("El módulo debe ser mayor a 0");
            });

            When(x => x.ControllerName != null, () =>
            {
                RuleFor(x => x.ControllerName)
                    .NotEmpty().WithMessage("El nombre del controlador no puede estar vacío")
                    .MaximumLength(100).WithMessage("El nombre del controlador no puede exceder 100 caracteres");
            });

            When(x => x.ActionName != null, () =>
            {
                RuleFor(x => x.ActionName)
                    .NotEmpty().WithMessage("El nombre de la acción no puede estar vacío")
                    .MaximumLength(100).WithMessage("El nombre de la acción no puede exceder 100 caracteres");
            });

            When(x => x.HttpMethod != null, () =>
            {
                RuleFor(x => x.HttpMethod)
                    .NotEmpty().WithMessage("El método HTTP no puede estar vacío")
                    .Must(x => new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }.Contains(x))
                    .WithMessage("El método HTTP debe ser GET, POST, PUT, DELETE o PATCH");
            });

            When(x => x.OperationKey != null, () =>
            {
                RuleFor(x => x.OperationKey)
                    .NotEmpty().WithMessage("La clave de operación no puede estar vacía")
                    .MaximumLength(200).WithMessage("La clave de operación no puede exceder 200 caracteres");
            });
        }
    }
}
