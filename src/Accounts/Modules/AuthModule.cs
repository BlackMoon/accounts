using System.Collections;
using System.Collections.Generic;
using accounts.Models;
using Nancy;

namespace Accounts.Modules
{
    public class AccountModule : NancyModule
    {
        public AccountModule()
        {
            Get[""] = Get["/"] = Get["/login"] = parameters =>
            {
                IEnumerable<SelectOption> tnsNames = new [] { new SelectOption("AQL.ECO"), new SelectOption("AQL.KPI") };
                return View["login", tnsNames];
            };

            Get["/logout"] = _ => View["home"];

            Post["/login"] = parameters => {
                
                return null;
            };
        }
    }
}
