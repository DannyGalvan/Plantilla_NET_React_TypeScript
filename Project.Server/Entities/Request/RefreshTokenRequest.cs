namespace Project.Server.Entities.Request
{
    /// <summary>
    /// Marker DTO. The refresh token travels in the HttpOnly cookie so the
    /// request body is empty; this class exists so Swagger documents the
    /// endpoint and so any future "rotate-by-body" fallback has a home.
    /// </summary>
    public class RefreshTokenRequest
    {
    }
}