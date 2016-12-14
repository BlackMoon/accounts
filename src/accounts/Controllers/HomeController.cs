using Kit.Core.Web.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace accounts.Controllers
{
    [SecurityHeaders]
    public class HomeController : Controller
    {
        [ResponseCache(CacheProfileName = "1hour")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
