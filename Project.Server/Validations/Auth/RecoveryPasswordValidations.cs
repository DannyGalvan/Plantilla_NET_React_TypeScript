using FluentValidation;
using Project.Server.Context;
using Project.Server.Entities.Request;

namespace Project.Server.Validations.Auth
{
    /// <summary>
    /// Defines the <see cref="RecoveryPasswordValidations" />
    /// </summary>
    public class RecoveryPasswordValidations : AbstractValidator<RecoveryPasswordRequest>
    {
        /// <summary>
        /// Defines the _db
        /// </summary>
        private readonly DataContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecoveryPasswordValidations"/> class.
        /// </summary>
        /// <param name="db">The db<see cref="DataContext"/></param>
        public RecoveryPasswordValidations(DataContext db)
        {
            _db = db;

            RuleFor(r => r.Email)
                .NotEmpty()
                .WithMessage("El Correo es obligatorio")
                .EmailAddress()
                .WithMessage("El Correo no es valido")
                .Must(UserDpiExists)
                .WithMessage("El Correo no existe");
        }

        /// <summary>
        /// The UserDpiExists
        /// </summary>
        /// <param name="email">The email<see cref="string"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private bool UserDpiExists(string email)
        {
            var user = _db.Users.Any(u => u.Email == email);

            return user;
        }
    }
}
