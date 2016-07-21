using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace accounts.UI.Logout
{
    [Authorize]
    public class LogoutController : Controller
    {
        private readonly IUserInteractionService _interaction;
        public LogoutController(IUserInteractionService interaction)
        {
            _interaction = interaction;
        }
        
        [HttpGet("ui/logout", Name = "Logout")]
        [ResponseCache(CacheProfileName = "1hour")]
        public IActionResult Index(string logoutId)
        {
            return View(new LogoutViewModel()
            {
                LogoutId = logoutId,
                Referer = Request.Headers["Referer"]
            });
        }
        
        [HttpPost("ui/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string logoutId)
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
    }
}
