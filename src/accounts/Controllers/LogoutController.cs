﻿using System.Security.Claims;
using System.Threading.Tasks;
using accounts.Models;
using IdentityServer4;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace accounts.Controllers
{
    [Authorize]
    public class LogoutController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IClientStore _clients;
        private readonly IIdentityServerInteractionService _interaction;
        
        public LogoutController(IClientStore clients, IIdentityServerInteractionService interaction, IConfiguration config)
        {
            _config = config;
            _clients = clients;
            _interaction = interaction;
        }

        private async Task<IActionResult> SignOut(string logoutId)
        {
            // delete authentication cookie
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
       
        [ResponseCache(CacheProfileName = "1hour")]
        public async Task<IActionResult> Index(string logoutId)
        {
            IActionResult result;

            bool enableSignOutPrompt;
            bool.TryParse(_config["EnableSignOutPrompt"], out enableSignOutPrompt);

            if (enableSignOutPrompt)
            {
                result = View(new LogoutViewModel()
                {
                    LogoutId = logoutId,
                    Referer = Request.Headers["Referer"]
                });
            }
            else
                result = await SignOut(logoutId);
                
            return result;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string logoutId)
        {
            return await SignOut(logoutId);
        }
    }
}
