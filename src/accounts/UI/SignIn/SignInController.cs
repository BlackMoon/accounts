using Microsoft.AspNet.Mvc;

namespace IdSvrHost.UI.SignIn
{
    public class SignInController : Controller
    {
        [HttpGet("ui/signin", Name ="SignIn")]
        public IActionResult Index(string id)
        {
            if (id != null)
            {
                return new SignInResult(id);
            }

            return Redirect("/");
        }
    }
}
