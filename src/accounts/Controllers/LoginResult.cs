using Kit.Dal.Oracle.Domain.Login.Command;

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
