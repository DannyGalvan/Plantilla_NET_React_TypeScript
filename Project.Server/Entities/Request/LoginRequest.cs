using System.ComponentModel.DataAnnotations;

namespace Project.Server.Entities.Request;

public class LoginRequest
{
    [Required(ErrorMessage = "UserName is required")]
    [StringLength(50, ErrorMessage = "UserName can't be longer than 50 characters")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(50, ErrorMessage = "Password can't be longer than 50 characters")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
