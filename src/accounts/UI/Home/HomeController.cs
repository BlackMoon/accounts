using System;
using Microsoft.AspNetCore.Mvc;

namespace accounts.UI.Home
{
    public class HomeController : Controller
    {
        [ResponseCache(CacheProfileName = "1hour")]
        [Route("/")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
