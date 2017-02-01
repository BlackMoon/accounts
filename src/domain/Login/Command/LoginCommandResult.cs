using Kit.Core.CQRS.Command;

namespace domain.Login.Command
{
    public class LoginCommandResult : ICommandResult
    {
        public LoginStatus Status { get; set; }

        public string Message { get; set; }
    }
}