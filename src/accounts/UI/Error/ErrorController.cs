using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;

namespace accounts.UI.Error
{
    public class ErrorController : Controller
    {
        private readonly IUserInteractionService _interaction;

        public ErrorController(IUserInteractionService interaction)
        {
            _interaction = interaction;
        }

        [HttpGet("ui/error" + "/{viewName}")]
        public IActionResult HandleUnknownAction(string viewName)
        {
            return View(viewName);
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
