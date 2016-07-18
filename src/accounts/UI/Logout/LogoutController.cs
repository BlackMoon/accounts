using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4;
using Microsoft.AspNetCore.Mvc;

namespace accounts.UI.Logout
{
    public class LogoutController : Controller
    {
        [HttpGet(Constants.RoutePaths.Logout, Name = "Logout")]
        public IActionResult Index(string returnUrl)
        {
            if (returnUrl != null && !Url.IsLocalUrl(returnUrl))
                returnUrl = null;
            
            return View(new LogoutViewModel
            {
                Referer = Request.Headers["Referer"],
                ReturnUrl = returnUrl
            });
        }

        [HttpPost(Constants.RoutePaths.Logout)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string signOutId)
        {
            await HttpContext.Authentication.SignOutAsync(Constants.PrimaryAuthenticationType);

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var vm = new LoggedOutViewModel();
            return View("LoggedOut", vm);
        }
    }
}
