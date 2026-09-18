namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Email sender contract.
    /// </summary>
    public interface ISendMail
    {
        Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default);
    }
}