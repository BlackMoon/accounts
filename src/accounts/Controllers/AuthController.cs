using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;

namespace accounts.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            ViewBag.Title = "Вход";

            ViewBag.TnsNames = new List<SelectListItem>() {
                new SelectListItem() { Text = "Источник данных", Selected = true },
                new SelectListItem() { Text = "AQL.ECO", Value = "AQL.ECO"}, 
                new SelectListItem() { Text = "AQL.KPI", Value = "AQL.KPI" }
            };

            return View();
        }
    }
}
