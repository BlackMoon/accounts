﻿using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;

namespace accounts.UI.Error
{
    public class ErrorController : Controller
    {
        private readonly ErrorInteraction _errorInteraction;

        public ErrorController(ErrorInteraction errorInteraction)
        {
            _errorInteraction = errorInteraction;
        }

        [HttpGet(Constants.RoutePaths.Error + "/{viewName}")]
        public IActionResult HandleUnknownAction(string viewName)
        {
            return View(viewName);
        }

        [Route(Constants.RoutePaths.Error, Name ="Error")]
        public async Task<IActionResult> Index(string id)
        {
            var vm = new ErrorViewModel();

            if (id != null)
            {
                var message = await _errorInteraction.GetRequestAsync(id);
                if (message != null)
                    vm.Error = message;
            }

            return View("Error", vm);
        }
    }
}
