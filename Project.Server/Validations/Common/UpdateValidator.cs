using FluentValidation;
using Project.Server.Entities.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Validations.Common
{
    /// <summary>
    /// Defines the <see cref="UpdateValidator{TEntity, TId}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public class UpdateValidator<TEntity, TId> : AbstractValidator<TEntity> where TEntity : class, IRequest<TId>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateValidator{TEntity, TId}"/> class.
        /// </summary>
        protected UpdateValidator()
        {
            RuleFor(x => x).NotNull();
            RuleFor(x => x.Id)
                .NotNull().WithMessage("El Id no puede ser nulo")
                .NotEmpty().WithMessage("El Id no puede ser vacío")
                .Must(Util.HasValidId).WithMessage("El Id no es valido");

            RuleFor(x => x.CreatedBy)
                .Null().WithMessage("El Usuario creador no puede ser modificado");

            RuleFor(x => x.UpdatedBy)
                .NotNull().WithMessage("El Usuario actualizador no puede ser nulo")
                .NotEmpty().WithMessage("El Usuario actualizador no puede ser vacío")
                .Must(Util.HasValidId).WithMessage("El Usuario actualizador no es valido");
        }
    }
}
