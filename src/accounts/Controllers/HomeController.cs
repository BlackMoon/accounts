using Kit.Core.Web.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace accounts.Controllers
{
    //[SecurityHeaders]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
