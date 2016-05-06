using Microsoft.AspNet.Mvc;

namespace accounts.UI.Home
{
    public class HomeController : Controller
    {
        [Route("/")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
