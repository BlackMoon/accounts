using Kit.Dal.CQRS.Command.Login;

namespace accounts
{
    /// <summary>
    /// Результат операции авторизации
    /// </summary>
    internal class LoginResult
    {
        public LoginStatus Status { get; set; }

        public string Message { get; set; }

        public string ReturnUrl { get; set; }
    }
}
