using System.ComponentModel.DataAnnotations;

namespace Figase.Models
{
    /// <summary>
    /// Модель авторизации
    /// </summary>
    public class LoginModel
    {
        /// <summary>
        /// Логин пользователя
        /// </summary>
        [Required, MinLength(1), MaxLength(256)]
        [Display(Name = "Логин")]
        public string Login { get; set; }

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        [Required, MinLength(1), MaxLength(256)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }
    }
}
