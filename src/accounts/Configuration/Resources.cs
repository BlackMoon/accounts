using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;

namespace accounts.Configuration
{
    public class Resources
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new []
            {
                new IdentityResources.OpenId(),
                new IdentityResource("connectionString", "Connection String", new[] {JwtClaimTypes.Name, "password", "datasource"})
            };
        }
    }
}