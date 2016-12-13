using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Services.InMemory;

namespace accounts.Services
{
    /// <summary>
    /// OpenId ProfileService для пользователей из БД
    /// </summary>
    public class ProfileService : IProfileService
    {
        public ProfileService(List<InMemoryUser> users)
        {
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var claims = new List<Claim>(context.Subject.Claims);
            /*if (!context.AllClaimsRequested)
            {
                claims = claims
                    .Where(x => context.RequestedClaimTypes.Contains(x.Type))
                    .ToList();
            }*/

            context.IssuedClaims = claims;

            return Task.FromResult(0);
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;

            return Task.FromResult(0);
        }
    }
}
