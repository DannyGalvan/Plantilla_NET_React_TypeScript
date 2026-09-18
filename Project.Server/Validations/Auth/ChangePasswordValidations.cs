using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Project.Server.Entities.Request;
using Project.Server.Services.Core;

namespace Project.Server.Validations.Auth
{
    /// <summary>
    /// Validates a password-change request. Strength is enforced through
    /// <see cref="SecurityPasswordPolicy"/> (B7) so the rule stays consistent
    /// across every endpoint that accepts a password.
    /// </summary>
    public class ChangePasswordValidations : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordValidations(IServiceProvider services)
        {
            RuleFor(c => c.Token).NotEmpty().WithMessage("Token is required.");

            RuleFor(c => c.Password)
                .NotEmpty().WithMessage("Password is required.");

            RuleFor(c => c)
                .Custom((req, ctx) =>
                {
                    if (req.Password != req.ConfirmPassword)
                    {
                        ctx.AddFailure(nameof(req.ConfirmPassword), "Passwords do not match.");
                    }

                    var policy = services.GetService<SecurityPasswordPolicy>() ?? new SecurityPasswordPolicy();
                    if (!policy.Validate(req.Password, out var message))
                    {
                        ctx.AddFailure(nameof(req.Password), message);
                    }
                });
        }
    }
}