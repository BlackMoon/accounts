using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kit.Dal.Configurations;
using Kit.Dal.CQRS.Command.ChangePassword;
using Kit.Dal.CQRS.Command.Login;
using Kit.Dal.CQRS.Query.TnsNames;
using Kit.Kernel.Configuration;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Query;
using Microsoft.AspNet.Authorization;
using Microsoft.Extensions.OptionsModel;

namespace accounts
{
    public class AuthController : Controller
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        
        private readonly AppSettings _appSettings;
        private readonly ConnectionStringSettings _connectionStringSettings;

        public AuthController(
            IQueryDispatcher queryDispatcher, 
            ICommandDispatcher commandDispatcher, 
            IOptions<AppSettings> appOptions,
            IOptions<ConnectionStringSettings> connectionStringOptions)
        {
            _queryDispatcher = queryDispatcher;
            _commandDispatcher = commandDispatcher;

            _appSettings = appOptions.Value;
            _connectionStringSettings = connectionStringOptions.Value;
        }

        [Authorize]
        public IActionResult ChangePassword(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = !string.IsNullOrEmpty(returnUrl) ? returnUrl : _appSettings.ReturnUrl;
            
            return PartialView("_ChangePassword");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, string returnUrl = null)
        {
            LoginResult result = new LoginResult() { Status = LoginStatus.Failure };

            if (ModelState.IsValid)
            {
                LoginCommandResult commandResult = _commandDispatcher.Dispatch<ChangePasswordCommand, LoginCommandResult>(command);
                
                result.Status = commandResult.Status;
                result.Message = commandResult.Message;
                result.ReturnUrl = returnUrl;
            }
            else
                result.Message = string.Join("; ",
                    ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                );

            return new JsonResult(result);
        }

        [AllowAnonymous, ResponseCache(Duration = 100)]
        public IActionResult Login(string returnUrl = null)
        {
            string theme = _appSettings.Theme;
            if (string.IsNullOrEmpty(theme))
            {
                string[] themes = {"red", "yellow", "orange", "green", "cyan", "blue", "pink", "pirple", "black"};

                int len = themes.Count(), month = DateTime.Today.Month;
                theme = themes[month % len];
            }

            ViewData["Theme"] = theme;
            ViewData["ReturnUrl"] = !string.IsNullOrEmpty(returnUrl) ? returnUrl : _appSettings.ReturnUrl;

            TnsNamesQueryResult result = _queryDispatcher.Dispatch<TnsNamesQuery, TnsNamesQueryResult>(
                new TnsNamesQuery() { ProviderInvariantName = _connectionStringSettings.ProviderName });

            ViewBag.TnsNames =
                new List<SelectListItem>()
                {
                    new SelectListItem() {Text = "Сервер", Value = string.Empty, Selected = true, Disabled = true}
                }
                .Union(result.Select(t => new SelectListItem() { Text = t, Value = t }));
          
            return View();
        }

        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginCommand command, string returnUrl = null)
        {
            LoginResult result = new LoginResult()
            {
                Status = LoginStatus.Failure
            };

            if (ModelState.IsValid)
            {
                LoginCommandResult commandResult = _commandDispatcher.Dispatch<LoginCommand, LoginCommandResult>(command);
                result.Status = commandResult.Status;
                result.Message = commandResult.Message;

                if (result.Status != LoginStatus.Failure)
                {
                    IList<Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, command.Login),
                        new Claim("password", command.Password),
                        new Claim("datasource", command.DataSource),
                        new Claim("lastlogindate", DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss.f")),
                    };

                    ClaimsIdentity id = new ClaimsIdentity(claims, "local");
                    await HttpContext.Authentication.SignInAsync("Cookies", new ClaimsPrincipal(id));
                }
            }
            else 
                result.Message = string.Join("; ", 
                    ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                );
            
            return new JsonResult(result);
        }
    }
}
