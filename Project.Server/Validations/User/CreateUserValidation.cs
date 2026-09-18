using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Project.Server.Entities.Request;
using Project.Server.Services.Core;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.User
{
    /// <summary>
    /// Validator for user creation. Password strength comes from
    /// <see cref="SecurityPasswordPolicy"/> (B7).
    /// </summary>
    public class CreateUserValidation : CreateValidator<UserRequest, long?>
    {
        public CreateUserValidation(IServiceProvider services)
        {
            RuleFor(x => x.RolId)
                .NotNull().WithMessage("Rol is required.")
                .GreaterThan(0).WithMessage("Rol must be a positive id.");

            RuleFor(x => x.Email)
                .NotNull().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not valid.")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");

            RuleFor(x => x.Name)
                .NotNull().WithMessage("Name is required.")
                .MaximumLength(150).WithMessage("Name cannot exceed 150 characters.");

            RuleFor(x => x.UserName)
                .NotNull().WithMessage("User name is required.")
                .MinimumLength(4).WithMessage("User name must be at least 4 characters.")
                .MaximumLength(50).WithMessage("User name cannot exceed 50 characters.");

            RuleFor(x => x)
                .Custom((req, ctx) =>
                {
                    var policy = services.GetService<SecurityPasswordPolicy>() ?? new SecurityPasswordPolicy();
                    if (!policy.Validate(req.Password, out var message))
                    {
                        ctx.AddFailure(nameof(req.Password), message);
                    }
                });

            RuleFor(x => x.IdentificationDocument)
                .MaximumLength(50).WithMessage("Identification document cannot exceed 50 characters.")
                .When(x => !string.IsNullOrEmpty(x.IdentificationDocument));

            RuleFor(x => x.Number)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
                .When(x => !string.IsNullOrEmpty(x.Number));

            RuleFor(x => x.State)
                .NotNull().WithMessage("State is required.")
                .InclusiveBetween(0, 1).WithMessage("State must be 0 (inactive) or 1 (active).");
        }
    }
}