using FluentValidation;
using Project.Server.Context;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;

namespace Project.Server.Validations.Auth
{
    using BC = BCrypt.Net.BCrypt;

    /// <summary>
    /// Defines the <see cref="ResetPasswordValidations" />
    /// </summary>
    public class ResetPasswordValidations : AbstractValidator<ResetPasswordRequest>
    {
        /// <summary>
        /// Defines the _db
        /// </summary>
        private readonly DataContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetPasswordValidations"/> class.
        /// </summary>
        /// <param name="db">The db<see cref="DataContext"/></param>
        public ResetPasswordValidations(DataContext db)
        {
            _db = db;

            RuleFor(u => u.IdUser)
               .NotEmpty()
               .WithMessage("Debes proporcionar el Id del usuario")
               .Must(UserExists)
               .WithMessage("El usuario no existe");
            RuleFor(u => u.Password)
                .NotEmpty()
                .WithMessage("Debes proporcionar una contraseña")
                .MinimumLength(8)
                .WithMessage("La contraseña debe contener al menos 8 caracteres")
                .Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).{8,25}$")
                .WithMessage("La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial, y tener entre 8 y 25 caracteres");
            RuleFor(u => u.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Debes confirmar la contraseña")
                .Equal(u => u.Password)
                .WithMessage("La confirmación de la contraseña debe coincidir con la contraseña");
            RuleFor(x => x).Custom((model, context) =>
            {
                User? user = _db.Users.FirstOrDefault(x => x.Id == model.IdUser);

                if (user != null)
                {
                    if (BC.Verify(model.Password, user.Password))
                    {
                        context.AddFailure("Password", $"La contraseña Actual no puede ser igual a la anterior");
                    }
                }
                else
                {
                    context.AddFailure("El usuario no existe");
                }
            });
        }

        /// <summary>
        /// The UserExists
        /// </summary>
        /// <param name="id">The id<see cref="long"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private bool UserExists(long id)
        {
            return _db.Users.Any(u => u.Id == id);
        }
    }
}
