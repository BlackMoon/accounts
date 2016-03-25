using System.Runtime.InteropServices;
using Nancy;
using Nancy.Security;

namespace Accounts.Modules
{
    public class AccountModule : NancyModule
    {
        public AccountModule()
        {
            Get[""] = Get["/"] = Get["/login"] = parameters =>
            {
                var model = new {title = "Hello, world!"};
                return View["login", model];
            };

            Get["/logout"] = _ => View["home"];

            Post["/login"] = parameters => {
                
                return null;
            };
        }
    }
}
