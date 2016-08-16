using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Kit.Kernel.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace accounts.UI.Error
{
    public class ErrorController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly IUserInteractionService _interaction;

        public ErrorController(IUserInteractionService interaction, IOptions<AppSettings> appSettings)
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
                string description = null;
                _appSettings.ErrorMessages?.TryGetValue(errorCode, out description);

                var message = new ErrorMessage() { ErrorCode = errorCode, ErrorDescription = description };
                vm.Error = message;
            };

            return View("Error", vm);
        }

        [Route("ui/error", Name = "Error")]
        public async Task<IActionResult> Index(string errorId)
        {
            var vm = new ErrorViewModel();

            if (errorId != null)
            {
                var message = await _interaction.GetErrorContextAsync(errorId);
                if (message != null)
                    vm.Error = message;
            }

            return View("Error", vm);
        }
    }
}
