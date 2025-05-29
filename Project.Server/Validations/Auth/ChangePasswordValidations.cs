using FluentValidation;
using Project.Server.Entities.Request;

namespace Project.Server.Validations.Auth
{
    /// <summary>
    /// Defines the <see cref="ChangePasswordValidations" />
    /// </summary>
    public class ChangePasswordValidations : AbstractValidator<ChangePasswordRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangePasswordValidations"/> class.
        /// </summary>
        public ChangePasswordValidations()
        {
            RuleFor(c => c.Token)
                .NotEmpty()
                .WithMessage("El token es obligatorio");
            RuleFor(c => c.ConfirmPassword)
                .NotEmpty()
                .WithMessage("La confirmación es obligatoria")
                .Equal(c => c.Password)
                .WithMessage("Las contraseñas no coinciden");
            RuleFor(c => c.Password)
                .NotEmpty()
                .WithMessage("la contraseña es obligatoria");
        }
    }
}
