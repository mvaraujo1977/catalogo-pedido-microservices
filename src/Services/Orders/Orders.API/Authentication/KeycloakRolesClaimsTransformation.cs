using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Orders.API.Authentication;

public class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim is null)
            return Task.FromResult(principal);

        using var json = JsonDocument.Parse(realmAccessClaim.Value);
        if (json.RootElement.TryGetProperty("roles", out var roles))
        {
            foreach (var role in roles.EnumerateArray())
            {
                var roleName = role.GetString();
                if (roleName is not null && !identity.HasClaim(ClaimTypes.Role, roleName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }

        return Task.FromResult(principal);
    }
}
