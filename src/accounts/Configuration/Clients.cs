using System.Collections.Generic;
using IdentityServer4.Core.Models;

namespace accounts.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    public static class Clients
    {
        public static IEnumerable<Client> Get()
        {
            return new List<Client>
            {
                /////////////////////////////////////////////////////////////
                // ECO WebForms OWIN Implicit Client
                /////////////////////////////////////////////////////////////
                new Client
                {
                    ClientName = "eco",
                    ClientId = "eco.webforms",

                    AllowedScopes = new List<string>
                    {
                        StandardScopes.OpenId.Name,
                        "connectionString"
                    },

                    RequireConsent = false,
                    AllowRememberConsent = false,

                    RedirectUris = new List<string>
                    {
                        "http://localhost:9930/"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "http://localhost:5969/"
                    }
                },

                /////////////////////////////////////////////////////////////
                // TNND WebForms OWIN Implicit Client
                /////////////////////////////////////////////////////////////
                new Client
                {
                    ClientName = "tnnd",
                    ClientId = "tnnd.webforms",

                    AllowedScopes = new List<string>
                    {
                        StandardScopes.OpenId.Name,
                        "connectionString"
                    },

                    RequireConsent = false,
                    AllowRememberConsent = false,

                    RedirectUris = new List<string>
                    {
                        "http://localhost:9930/"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "http://localhost:5969/"
                    }
                },

                /////////////////////////////////////////////////////////////
                // Test WebForms OWIN Implicit Client
                /////////////////////////////////////////////////////////////
                new Client
                {
                    ClientName = "test",
                    ClientId = "test",

                    AllowedScopes = new List<string>
                    {
                        StandardScopes.OpenId.Name,
                        "connectionString"
                    },

                    RequireConsent = false,
                    AllowRememberConsent = false,

                    RedirectUris = new List<string>
                    {
                        "http://localhost:16011"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "http://localhost:5969/"
                    }
                },

                new Client
                {
                    ClientName = "test1",
                    ClientId = "test1",

                    AllowedScopes = new List<string>
                    {
                        StandardScopes.OpenId.Name,
                        "connectionString"
                    },

                    RequireConsent = false,
                    AllowRememberConsent = false,

                    RedirectUris = new List<string>
                    {
                        "http://localhost:9930"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "http://localhost:5969/"
                    }
                }
            };
        }
    }
}