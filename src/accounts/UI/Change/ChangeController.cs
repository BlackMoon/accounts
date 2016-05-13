using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using accounts.UI.Login;
using IdentityServer4.Core;
using Kit.Dal.CQRS.Command.ChangePassword;
using Kit.Dal.CQRS.Command.Login;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.Identity;
using Kit.Kernel.Web.Http.Ajax;
using Kit.Kernel.Web.Mvc;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace accounts.UI.Change
{
    public class ChangeController : Controller
    {
        private readonly ICommandDispatcher _commandDispatcher;

        public ChangeController(ICommandDispatcher commandDispatcher)
        {
            _commandDispatcher = commandDispatcher;
        }

        [Authorize]
        [HttpGet("ui/change", Name = "Change")]
        public IActionResult Index(string id)
        {
            ChangePasswordCommand command = new ChangePasswordCommand() { SignInId = id };

            return Request.IsAjax() ? (IActionResult) PartialView(command) : View(command);
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
                    result.ReturnUrl = (command.SignInId != null) ? "/ui/signin?id=" + command.SignInId : "/";

                    await HttpContext.Authentication.SignOutAsync(Constants.PrimaryAuthenticationType);

                    ClaimsIdentity ci = HttpContext.User.Identity as ClaimsIdentity;
                    if (ci != null)
                    {
                        Claim claimPsw = ci.FindFirst(ConnectionStringClaimTypes.Password);

                        ci.TryRemoveClaim(claimPsw);
                        ci.AddClaim(new Claim(ConnectionStringClaimTypes.Password, command.NewPassword));

                        await HttpContext.Authentication.SignInAsync(Constants.PrimaryAuthenticationType, new ClaimsPrincipal(ci));
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
