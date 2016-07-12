using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Services;
using Kit.Dal.Configurations;
using Kit.Dal.CQRS.Command.Login;
using Kit.Dal.CQRS.Query.TnsNames;
using Kit.Kernel.Configuration;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Query;
using Kit.Kernel.Identity;
using Microsoft.Extensions.Options;
using Kit.Kernel.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace accounts.UI.Login
{
    public class LoginController : Controller
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly SignInInteraction _signInInteraction;
       
        private readonly ConnectionStringSettings _connectionStringSettings;

        public LoginController(
            ICommandDispatcher commandDispatcher,
            IQueryDispatcher queryDispatcher,
            IOptions<ConnectionStringSettings> connectionStringOptions,
            SignInInteraction signInInteraction)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _signInInteraction = signInInteraction;
            _connectionStringSettings = connectionStringOptions.Value;
        }

        [HttpGet(Constants.RoutePaths.Login, Name = "Login")]
        public async Task<IActionResult> Index(string id)
        {
            // default DataSource задан в настройках
            if (string.IsNullOrEmpty(_connectionStringSettings.DataSource))
            {
                TnsNamesQueryResult result = _queryDispatcher.Dispatch<TnsNamesQuery, TnsNamesQueryResult>(new TnsNamesQuery() { ProviderInvariantName = _connectionStringSettings.ProviderName });

                ViewBag.TnsNames = new List<SelectListItem>()
                    {
                        new SelectListItem()
                        {
                            Text = "Сервер",
                            Value = string.Empty,
                            Selected = true,
                            Disabled = true
                        }
                    }
                    .Union(result.Select(t => new SelectListItem() {Text = t, Value = t}));
            }

            LoginCommand command = new LoginCommand();

            if (id != null)
            {
                var request = await _signInInteraction.GetRequestAsync(id);
                if (request != null)
                {
                    command.UserName = request.LoginHint;
                    command.SignInId = id;
                }
            }

            return View(command);
        }

        [HttpPost(Constants.RoutePaths.Login)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginCommand command)
        {
            #region если Default DataSource задан в настройках
            if (!string.IsNullOrEmpty(_connectionStringSettings.DataSource))
            {
                command.DataSource = _connectionStringSettings.DataSource;

                ModelStateEntry mse;
                ModelState.TryGetValue("DataSource", out mse);
                if (mse != null)
                {
                    mse.Errors.Clear();
                    mse.ValidationState = ModelValidationState.Valid;
                }
            }
            #endregion

            LoginResult result = new LoginResult() { Status = LoginStatus.Failure };
            
            if (ModelState.IsValid)
            {
                LoginCommandResult commandResult = _commandDispatcher.Dispatch<LoginCommand, LoginCommandResult>(command);
                result.Status = commandResult.Status;
                result.Message = commandResult.Message;

                // Authenticate
                if (result.Status != LoginStatus.Failure)
                {
                    Claim[] claims = {
                            new Claim(JwtClaimTypes.Name, command.UserName),
                            new Claim(ConnectionStringClaimTypes.Password, command.Password),
                            new Claim(ConnectionStringClaimTypes.DataSource, command.DataSource),
                            new Claim(JwtClaimTypes.Subject, command.DataSource + command.UserName + command.Password),
                            new Claim(JwtClaimTypes.IdentityProvider, "idsvr"),
                            new Claim(JwtClaimTypes.AuthenticationTime, DateTime.UtcNow.ToEpochTime().ToString()),
                        };

                    ClaimsIdentity ci = new ClaimsIdentity(claims, "password", JwtClaimTypes.Name, JwtClaimTypes.Role);
                    ClaimsPrincipal cp = new ClaimsPrincipal(ci);

                    await HttpContext.Authentication.SignInAsync(Constants.PrimaryAuthenticationType, cp);

                    // если LoginStatus.Expired --> Redirect /ui/change (на клиенте)
                    if (result.Status != LoginStatus.Expired)
                        result.ReturnUrl = (command.SignInId != null) ? "/ui/signin?id=" + command.SignInId : "/";
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