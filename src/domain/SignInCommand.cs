

namespace domain
{
    public class SignInCommand : Kit.Core.CQRS.Command.ICommand
    {
        /// <summary>
        /// Identity ReturnUrl (аутентификация через OpenId)
        /// </summary>
        public string ReturnUrl { get; set; } = "/";
    }
}
