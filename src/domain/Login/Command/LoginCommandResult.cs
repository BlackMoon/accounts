using Kit.Core.CQRS.Command;

namespace Kit.Dal.Oracle.Domain.Login.Command
{
    public class LoginCommandResult : ICommandResult
    {
        public LoginStatus Status { get; set; }

        public string Message { get; set; }
    }
}