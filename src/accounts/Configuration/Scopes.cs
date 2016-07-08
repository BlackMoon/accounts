using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;

namespace accounts.Configuration
{
    public class Scopes
    {
        public static IEnumerable<Scope> Get()
        {
            return new List<Scope>
            {
                StandardScopes.OpenId,

                new Scope
                {
                    Name = "connectionString",
                    DisplayName = "Connection String",
                    
                    Required = true,
                    Type = ScopeType.Identity,

                    ScopeSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha256())
                    },

                    Claims = new List<ScopeClaim>
                    {
                        new ScopeClaim(JwtClaimTypes.Name, true),
                        new ScopeClaim("password", true),
                        new ScopeClaim("datasource", true)
                    }
                }
            };
        }
    }
}