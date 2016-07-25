﻿using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace accounts.UI.Logout
{
    [Authorize]
    public class LogoutController : Controller
    {
        private readonly IUserInteractionService _interaction;
        private readonly IdentityServerOptions _options;
        public LogoutController(IUserInteractionService interaction, IdentityServerOptions options)
        {
            _interaction = interaction;
            _options = options;
        }

        private async Task<IActionResult> SignOut(string logoutId)
        {
            await HttpContext.Authentication.SignOutAsync(Constants.DefaultCookieAuthenticationScheme);

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel()
            {
                PostLogoutRedirectUri = logout.PostLogoutRedirectUri,
                ClientName = logout.ClientId,
                SignOutIframeUrl = logout.SignOutIFrameUrl
            };
            return View("LoggedOut", vm);
        }

        [HttpGet("ui/logout", Name = "Logout")]
        [ResponseCache(CacheProfileName = "1hour")]
        public async Task<IActionResult> Index(string logoutId)
        {
            IActionResult result;

            if (_options.AuthenticationOptions.EnableSignOutPrompt)
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

        [HttpPost("ui/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string logoutId)
        {
            return await SignOut(logoutId);
        }
    }
}
