using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Services;
using Kit.Core.CQRS.Command;
using Kit.Core.CQRS.Query;
using Kit.Core.Identity;
using Kit.Core.Web.Mvc;
using Kit.Core.Web.Mvc.Filters;
using Kit.Dal.Configurations;
using Kit.Dal.CQRS.Command.Login;
using Kit.Dal.CQRS.Query.TnsNames;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace accounts.Controllers
{
    [SecurityHeaders(Directive = "script-src 'self' 'unsafe-eval' 'sha256-/dselSWiKLD2SUSXKnFwLDhDtLSAEW4yzXCfaDrhkZE='")]
    public class LoginController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IIdentityServerInteractionService _interaction;

        private readonly ConnectionStringSettings _connectionStringSettings;

        public LoginController(
            ICommandDispatcher commandDispatcher,
            IQueryDispatcher queryDispatcher,
            IOptions<ConnectionStringSettings> connectionStringOptions,
            IIdentityServerInteractionService interaction,
            IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _interaction = interaction;
            _connectionStringSettings = connectionStringOptions.Value;
        }
        
        [HttpGet]
        [ResponseCache(CacheProfileName = "1hour")]
        public async Task<IActionResult> Index(string returnUrl)
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
                    .Concat(result.Select(t => new SelectListItem() {Text = t, Value = t}));
            }
            
            
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            LoginCommand command = new LoginCommand
            {
                UserName = context?.LoginHint,
                ReturnUrl = _interaction.IsValidReturnUrl(returnUrl) ? returnUrl : "/"
            };

            return View(command);
        }

        [HttpPost]
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
            
            LoginCommandResult result = new LoginCommandResult();
            
            if (ModelState.IsValid)
            {
                result = _commandDispatcher.Dispatch<LoginCommand, LoginCommandResult>(command);
               
                // Authenticate
                if (result.Status != LoginStatus.Failure)
                {
                    Claim[] claims = {
                            new Claim(JwtClaimTypes.Name, command.UserName),
                            new Claim(ConnectionStringClaimTypes.Password, command.Password),
                            new Claim(ConnectionStringClaimTypes.DataSource, command.DataSource),
                            new Claim(JwtClaimTypes.Subject, command.DataSource + command.UserName + command.Password),
                            new Claim(JwtClaimTypes.IdentityProvider, "idsvr"),
                            new Claim(JwtClaimTypes.AuthenticationTime, DateTime.UtcNow.ToEpochTime().ToString())
                        };

                    ClaimsIdentity ci = new ClaimsIdentity(claims, "password", JwtClaimTypes.Name, JwtClaimTypes.Role);

                    // persistent cookie
                    AuthenticationProperties props = new AuthenticationProperties
                    {
                        IsPersistent = _appSettings.Persistent,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(_appSettings.Timeout)
                    };

                    await HttpContext.Authentication.SignInAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme, new ClaimsPrincipal(ci), props);
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