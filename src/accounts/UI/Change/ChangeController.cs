using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using accounts.UI.Login;
using IdentityServer4;
using Kit.Dal.CQRS.Command.ChangePassword;
using Kit.Dal.CQRS.Command.Login;
using Kit.Core.CQRS.Command;
using Kit.Core.Identity;
using Kit.Core.Web.Http.Ajax;
using Kit.Core.Web.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace accounts.UI.Change
{
    [Authorize]
    public class ChangeController : Controller
    {
        private readonly ICommandDispatcher _commandDispatcher;

        public ChangeController(ICommandDispatcher commandDispatcher)
        {
            _commandDispatcher = commandDispatcher;
        }
        
        [HttpGet("ui/change", Name = "Change")]
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

        [HttpPost("ui/change")]
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
                    result.ReturnUrl = command.ReturnUrl ?? "/";
                    await HttpContext.Authentication.SignOutAsync(Constants.DefaultCookieAuthenticationScheme);

                    ClaimsIdentity ci = HttpContext.User.Identity as ClaimsIdentity;
                    if (ci != null)
                    {
                        Claim claimPsw = ci.FindFirst(ConnectionStringClaimTypes.Password);

                        ci.TryRemoveClaim(claimPsw);
                        ci.AddClaim(new Claim(ConnectionStringClaimTypes.Password, command.NewPassword));
                        
                        await HttpContext.Authentication.SignInAsync(Constants.DefaultCookieAuthenticationScheme, new ClaimsPrincipal(ci));
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
