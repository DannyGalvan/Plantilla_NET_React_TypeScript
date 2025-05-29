namespace Project.Server.Entities.Request;

public class ResetPasswordRequest
{
    public long IdUser { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
