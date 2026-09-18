namespace Project.Server.Entities.Response
{
    /// <summary>
    /// Returned alongside the access token so the client knows when to refresh.
    /// The refresh token itself is NOT carried in this body — it travels in
    /// the HttpOnly cookie.
    /// </summary>
    public class AuthWithRefreshResponse : AuthResponse
    {
        /// <summary>Seconds until the access token expires.</summary>
        public int ExpiresInSeconds { get; set; }
    }
}