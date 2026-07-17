using DukkanPilot.Core.Enums;
namespace DukkanPilot.IntegrationTests.Infrastructure;
public enum TestUser { Admin, TenantAOwner, TenantAStaff, TenantBOwner, TenantBStaff }
public static class TestClaims
{
 public static HttpClient CreateClient(DukkanPilotWebApplicationFactory f, TestUser u)
 {
  var c=f.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions{AllowAutoRedirect=false}); var d=f.Data;
  P p=u switch { TestUser.Admin=>new(d.AdminUserId,"Admin","admin.integration@dukkanpilot.test",UserRole.SuperAdmin,(int?)null,(BusinessRole?)null), TestUser.TenantAOwner=>new(d.TenantAOwnerUserId,"A Owner","owner.a@dukkanpilot.test",UserRole.BusinessOwner,d.TenantAId,BusinessRole.Owner), TestUser.TenantAStaff=>new(d.TenantAStaffUserId,"A Staff","staff.a@dukkanpilot.test",UserRole.Staff,d.TenantAId,BusinessRole.Staff), TestUser.TenantBOwner=>new(d.TenantBOwnerUserId,"B Owner","owner.b@dukkanpilot.test",UserRole.BusinessOwner,d.TenantBId,BusinessRole.Owner), _=>new(d.TenantBStaffUserId,"B Staff","staff.b@dukkanpilot.test",UserRole.Staff,d.TenantBId,BusinessRole.Staff)};
  void H(string n,object v)=>c.DefaultRequestHeaders.Add(n,v.ToString()); H(TestAuthenticationHandler.UserIdHeader,p.Id);H(TestAuthenticationHandler.NameHeader,p.Name);H(TestAuthenticationHandler.EmailHeader,p.Email);H(TestAuthenticationHandler.RoleHeader,p.Role); if(p.BusinessId is not null){H(TestAuthenticationHandler.BusinessIdHeader,p.BusinessId.Value);H(TestAuthenticationHandler.BusinessRoleHeader,p.BusinessRole!.Value);} return c;
 }
 private sealed record P(int Id,string Name,string Email,UserRole Role,int? BusinessId,BusinessRole? BusinessRole);
}
