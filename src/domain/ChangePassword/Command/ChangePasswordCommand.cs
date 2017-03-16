using System.ComponentModel.DataAnnotations;
using Kit.Core.Encryption;

namespace domain.ChangePassword.Command
{
    public class ChangePasswordCommand : SignInCommand
    {
        [EncryptDataType(DataType.Password)]
        [Required(ErrorMessage = "Введите новый пароль")]
        public string NewPassword { get; set; }

        [EncryptDataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Новый пароль не совпадает с подтверждением")]
        public string ConfirmPassword { get; set; }
    }
}
