using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;


namespace FileIngestor.API.Authorizations
{
    public class RoleBasedAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
        {
            // Política de autorización.
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }
    
            context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
