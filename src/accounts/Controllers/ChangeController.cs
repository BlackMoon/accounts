using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using domain.ChangePassword.Command;
using domain.Login.Command;
using IdentityServer4;
using Kit.Core.CQRS.Command;
using Kit.Core.Identity;
using Kit.Core.Web.Http.Ajax;
using Kit.Core.Web.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace accounts.Controllers
{
    [Authorize]
    public class ChangeController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly ICommandDispatcher _commandDispatcher;

        public ChangeController(ICommandDispatcher commandDispatcher, IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _commandDispatcher = commandDispatcher;
        }
        
        [HttpGet]
        [ResponseCache(CacheProfileName = "1hour")]
        public IActionResult Index(string returnUrl)
        {
            ChangePasswordCommand command = new ChangePasswordCommand() { ReturnUrl = returnUrl };
            
            if (Request.IsAjax())
                return PartialView(command);

            // в полном представлении необходим Layout
            ViewBag.Layout = "_FormLayout";
            return View(command);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ChangePasswordCommand command)
        {
            LoginResult result = new LoginResult() { Status = LoginStatus.Failure };

            if (ModelState.IsValid)
            {
                LoginCommandResult commandResult = _commandDispatcher.Dispatch<ChangePasswordCommand, LoginCommandResult>(command);
                result.Status = commandResult.Status;
                result.Message = commandResult.Message;

                if (result.Status != LoginStatus.Failure)
                {
                    result.ReturnUrl = command.ReturnUrl ?? "~/";
                    await HttpContext.Authentication.SignOutAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme);

                    ClaimsIdentity ci = HttpContext.User.Identity as ClaimsIdentity;
                    if (ci != null)
                    {
                        Claim claimPsw = ci.FindFirst(ConnectionClaimTypes.Password);

                        ci.TryRemoveClaim(claimPsw);
                        ci.AddClaim(new Claim(ConnectionClaimTypes.Password, command.NewPassword));

                        // persistent cookie
                        AuthenticationProperties props = new AuthenticationProperties
                        {
                            IsPersistent = _appSettings.Persistent,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(_appSettings.Timeout)
                        };

                        await HttpContext.Authentication.SignInAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme, new ClaimsPrincipal(ci), props);
                    }
                }
            }
            else
                result.Message = string.Join("; ",
                    ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                );

            return new JsonResultIe(result);
        }
    }
}
