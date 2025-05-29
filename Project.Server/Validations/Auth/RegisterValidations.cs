using FluentValidation;
using Project.Server.Entities.Request;
using Project.Server.Validations.Common;

namespace Project.Server.Validations.Auth
{
    public class RegisterValidations : CreateValidator<RegisterRequest,long?>
    {
        public RegisterValidations()
        {
            RuleFor(x => x.State)
                .NotEmpty().WithMessage("El estado no puede ser vacio");
            RuleFor(x => x.CountryCode)
                .NotEmpty().WithMessage("El Codigo de pais no puede ser vacio");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El Correo electronico no puede ser vacio");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre no puede ser vacio");
            RuleFor(x => x.Number)
                .NotEmpty().WithMessage("El numero no puede ser vacio")
                .MinimumLength(11).WithMessage("El numero debe tener minimo 11 caracteres");
            RuleFor(l => l.Password)
                .NotEmpty().WithMessage("la contrase;a no puede ser vacia")
                .MinimumLength(8).WithMessage("La contraseña debe contener al menos 8 caracteres")
                .Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).{8,25}$")
                .WithMessage("La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial, y tener entre 8 y 25 caracteres");
            RuleFor(x => x.IdentificationDocument)
                .NotEmpty().WithMessage("El documento de indentificacion no puede ser vacio")
                .MinimumLength(8).WithMessage("El documento de identificacion debe tener por lo menos 8 caracteres");
        }
    }
}
