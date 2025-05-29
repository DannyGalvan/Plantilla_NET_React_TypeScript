using FluentValidation;
using Project.Server.Entities.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Validations.Common
{
    /// <summary>
    /// Defines the <see cref="PartialUpdateValidator{TEntity, TId}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public class PartialUpdateValidator<TEntity, TId> : AbstractValidator<TEntity> where TEntity : class, IRequest<TId>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialUpdateValidator{TEntity, TId}"/> class.
        /// </summary>
        protected PartialUpdateValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("El Id de la entidad es requerido")
                .Must(Util.HasValidId).WithMessage("El Id no tiene un formato valido");
            RuleFor(x => x.CreatedBy)
                .Null().WithMessage("El Usuario creador no puede ser modificado");
            RuleFor(x => x.UpdatedBy)
                .NotEmpty().WithMessage("El Usuario actualizador es requerido")
                .Must(Util.HasValidId).WithMessage("El Usuario creador no es valido");
        }
    }
}
