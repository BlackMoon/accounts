using Microsoft.AspNetCore.Mvc;

namespace accounts.UI.SignIn
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
