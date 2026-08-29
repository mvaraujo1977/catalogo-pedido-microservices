using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Handler de autenticação fake para os testes de integração. Substitui
/// apenas o esquema de autenticação (não valida nenhum token JWT) — a
/// policy "AdminOnly" e o [Authorize] continuam sendo avaliados de
/// verdade pelo pipeline de Authorization real, exatamente como em
/// produção. A identidade simulada é controlada pelo header
/// "X-Test-Role" da requisição.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var roleValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = roleValues.ToString();

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, $"{role.ToLowerInvariant()}-test"),
            new Claim(ClaimTypes.Role, role),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
