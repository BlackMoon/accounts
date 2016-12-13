using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Kit.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace accounts.UI.Error
{
    public class ErrorController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly IIdentityServerInteractionService _interaction;

        public ErrorController(IIdentityServerInteractionService interaction, IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _interaction = interaction;
        }

        [HttpGet("ui/error" + "/{errorCode}")]
        [ResponseCache(CacheProfileName = "1hour")]
        public IActionResult HandleUnknownAction(string errorCode)
        {
            var vm = new ErrorViewModel();

            if (!string.IsNullOrEmpty(errorCode))
            {
                ErrorMessage message = new ErrorMessage() { Error = errorCode };
                vm.Error = message;

                string description = null;
                _appSettings.ErrorMessages?.TryGetValue(errorCode, out description);
                ViewBag.ErrorDescription = description;
            };

            return View("Error", vm);
        }

        [Route("ui/error", Name = "Error")]
        public async Task<IActionResult> Index(string errorId)
        {
            var vm = new ErrorViewModel();

            if (errorId != null)
            {
                ErrorMessage message = await _interaction.GetErrorContextAsync(errorId);
                if (message != null)
                    vm.Error = message;
            }

            return View("Error", vm);
        }
    }
}
