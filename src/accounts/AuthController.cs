using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kit.Dal.CQRS.Command.Login;
using Kit.Dal.CQRS.Query.TnsNames;
using Kit.Kernel.Configuration;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Query;
using Kit.Kernel.Web.Ajax;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.Extensions.OptionsModel;
using Newtonsoft.Json;

namespace accounts
{
    public class AuthController : Controller
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        
        private readonly AppSettings _options;

        public AuthController(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher, IOptions<AppSettings> options)
        {
            _queryDispatcher = queryDispatcher;
            _commandDispatcher = commandDispatcher;

            _options = options.Value;
        }

        //[/*Authorize,*/]
        public IActionResult ChangePassword(string returnUrl = null)
        {
            ModelState.AddModelError("expired", "Срок действия Вашего пароля истек");
            return View();
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = !string.IsNullOrEmpty(returnUrl) ? returnUrl : _options.ReturnUrl;

            TnsNamesQueryResult result = _queryDispatcher.Dispatch<TnsNamesQuery, TnsNamesQueryResult>(
                new TnsNamesQuery() { ProviderInvariantName = "Oracle.DataAccess.Client" });
            
            ViewBag.TnsNames =
                new List<SelectListItem>() { new SelectListItem() { Text = "Источник данных", Value = string.Empty, Selected = true } }
                .Union(result.Select(t => new SelectListItem() { Text = t, Value = t }));
            
            return View();
        }

        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginCommand command, string returnUrl = null)
        {
            string msg = string.Empty;
            LoginStatus status = LoginStatus.Failure;

            if (ModelState.IsValid)
            {
                LoginCommandResult result = _commandDispatcher.Dispatch<LoginCommand, LoginCommandResult>(command);
                status = result.Status;

                if (status != LoginStatus.Failure)
                {
                    IList<Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, command.Login),
                        new Claim("password", command.Password),
                        new Claim("datasource", command.DataSource),
                        new Claim("lastlogindate", DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss.f")),
                    };

                    var id = new ClaimsIdentity(claims, "local");
                    Task task = HttpContext.Authentication.SignInAsync("Cookies", new ClaimsPrincipal(id));
                    
                    // смена пароля 
                    if (status == LoginStatus.Expired || status == LoginStatus.Expiring)
                        returnUrl = "/change?" + returnUrl;

                    await task;
                }
            }
            else
                msg = string.Join("</li><li>", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));

            return new JsonResult(new { status, message = msg, returnUrl });
        }
    }
}
