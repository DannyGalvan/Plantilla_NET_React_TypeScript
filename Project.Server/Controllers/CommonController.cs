using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Project.Server.Controllers
{
    /// <summary>
    /// Defines the <see cref="CommonController" />
    /// </summary>
    public class CommonController : ControllerBase
    {
        /// <summary>
        /// The GetUserId
        /// </summary>
        /// <returns>The <see cref="long"/></returns>
        protected long GetUserId()
        {
            Claim? claimId = User.FindFirst(ClaimTypes.NameIdentifier);

            return claimId != null ? long.Parse(claimId.Value) : 0;
        }
    }
}
