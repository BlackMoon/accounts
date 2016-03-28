using System.ComponentModel.DataAnnotations;

namespace accounts.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Description = "Источник данных")]
        public string DataSource { get; set; }

        [Required]
        [Display(Description = "Имя пользователя")]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Description = "Пароль")]
        public string Password { get; set; }
    }
}
