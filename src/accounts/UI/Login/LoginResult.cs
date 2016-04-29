using Kit.Dal.CQRS.Command.Login;

namespace IdSvrHost.UI.Login
{
    /// <summary>
    /// Результат операции авторизации
    /// </summary>
    internal class LoginResult : LoginCommandResult
    {
        public string ReturnUrl { get; set; }
    }
}
