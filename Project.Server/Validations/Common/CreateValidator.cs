using FluentValidation;
using Project.Server.Entities.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Validations.Common
{
    /// <summary>
    /// Defines the <see cref="CreateValidator{TEntity, TId}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public class CreateValidator<TEntity, TId> : AbstractValidator<TEntity> where TEntity : class, IRequest<TId>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateValidator{TEntity, TId}"/> class.
        /// </summary>
        protected CreateValidator()
        {
            RuleFor(x => x)
                .NotNull().WithMessage("La entidad no puede ser nula");
            RuleFor(x => x.Id)
                .Null().WithMessage("El Id No debes mandarlo al crear una nueva entidad");

            RuleFor(x => x.CreatedBy)
                .NotNull().WithMessage("El Usuario creador no puede ser nulo")
                .NotEmpty().WithMessage("El Usuario creador no puede ser vacío")
                .Must(Util.HasValidId).WithMessage("El Usuario creador no es valido");
        }
    }
}
