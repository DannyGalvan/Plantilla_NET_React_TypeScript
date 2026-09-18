using FluentValidation;
using Project.Server.Entities.Request;

namespace Project.Server.Validations.Auth
{
    /// <summary>
    /// Validates the recovery request payload only. Existence of the email is
    /// intentionally NOT checked here (B16 — account enumeration): the
    /// controller returns a generic 200 either way.
    /// </summary>
    public class RecoveryPasswordValidations : AbstractValidator<RecoveryPasswordRequest>
    {
        public RecoveryPasswordValidations()
        {
            RuleFor(r => r.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not valid.");
        }
    }
}