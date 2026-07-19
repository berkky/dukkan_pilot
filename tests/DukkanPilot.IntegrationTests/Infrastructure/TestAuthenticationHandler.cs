using System.Security.Claims;
using System.Text.Encodings.Web;
using DukkanPilot.Web.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
namespace DukkanPilot.IntegrationTests.Infrastructure;
public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName="IntegrationTest", UserIdHeader="X-Test-UserId", NameHeader="X-Test-Name", EmailHeader="X-Test-Email", RoleHeader="X-Test-Role", BusinessIdHeader="X-Test-BusinessId", BusinessRoleHeader="X-Test-BusinessRole";
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e):base(o,l,e) { }
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if(!Request.Headers.TryGetValue(UserIdHeader,out var id)||!Request.Headers.TryGetValue(RoleHeader,out var role)) return Task.FromResult(AuthenticateResult.NoResult());
        var c=new List<Claim>{new(ClaimTypes.NameIdentifier,id.ToString()),new(ClaimTypes.Name,Request.Headers[NameHeader].ToString()),new(ClaimTypes.Email,Request.Headers[EmailHeader].ToString()),new(ClaimTypes.Role,role.ToString())};
        if(Request.Headers.TryGetValue(BusinessIdHeader,out var bid)) c.Add(new(AuthClaimTypes.BusinessId,bid.ToString()));
        if(Request.Headers.TryGetValue(BusinessRoleHeader,out var br)) c.Add(new(AuthClaimTypes.BusinessRole,br.ToString()));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(c,SchemeName)),SchemeName)));
    }
}
