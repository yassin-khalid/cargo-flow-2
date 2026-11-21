using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CargoFlow.IntegrationTests;

public class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestScheme";
    public const string UserIdHeader = "X-Test-UserId";
    public const string RoleHeader = "X-Test-Role";
    public const string ProfileIdHeader = "X-Test-ProfileId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        List<Claim> claims = [
            new(ClaimTypes.NameIdentifier, userId.ToString())
        ];
        if (Request.Headers.TryGetValue(RoleHeader, out var roleValues))
        {
            var role = roleValues.ToString();
            claims.Add(new(ClaimTypes.Role, role));
            claims.Add(new("user_type", role));

            if (Request.Headers.TryGetValue(ProfileIdHeader, out var profileId))
            {
                var profileClaimType = role switch
                {
                    "Customer" => "customer_id",
                    "Company" => "company_id",
                    _ => null
                };

                if (profileClaimType is not null)
                {
                    claims.Add(new(profileClaimType, profileId.ToString()));
                }
            }

        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
