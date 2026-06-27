using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Watchtower.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("User ID claim is missing.");
        return Guid.Parse(value);
    }
}
