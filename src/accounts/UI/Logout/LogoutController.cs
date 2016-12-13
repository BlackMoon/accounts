﻿using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Kit.Core.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace accounts.UI.Logout
{
    [Authorize]
    public class LogoutController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly IClientStore _clients;
        private readonly IIdentityServerInteractionService _interaction;
        
        public LogoutController(IClientStore clients, IIdentityServerInteractionService interaction, IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _clients = clients;
            _interaction = interaction;
        }

        private async Task<IActionResult> SignOut(string logoutId)
        {
            await HttpContext.Authentication.SignOutAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme);

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var logout = await _interaction.GetLogoutContextAsync(logoutId);
            var client = await _clients.FindClientByIdAsync(logout.ClientId);
            
            var vm = new LoggedOutViewModel()
            {
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = client?.ClientName ?? logout?.ClientId,
                SignOutIframeUrl = logout?.SignOutIFrameUrl
            };
            return View("LoggedOut", vm);
        }

        [HttpGet("ui/logout", Name = "Logout")]
        [ResponseCache(CacheProfileName = "1hour")]
        public async Task<IActionResult> Index(string logoutId)
        {
            IActionResult result;

            /*if (_appSettings.options.AuthenticationOptions.EnableSignOutPrompt)
            {
                result = View(new LogoutViewModel()
                {
                    LogoutId = logoutId,
                    Referer = Request.Headers["Referer"]
                });
            }
            else*/
                result = await SignOut(logoutId);
                
            return result;
        }

        [HttpPost("ui/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string logoutId)
        {
            return await SignOut(logoutId);
        }
    }
}
