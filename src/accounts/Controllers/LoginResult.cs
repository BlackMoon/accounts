using Kit.Dal.CQRS.Command.Login;

namespace accounts.Controllers
{
    /// <summary>
    /// Результат операции авторизации
    /// </summary>
    internal class LoginResult : LoginCommandResult
    {
        public string ReturnUrl { get; set; }
    }
}
