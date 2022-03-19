using Figase.Enums;
using System.ComponentModel.DataAnnotations;

namespace Figase.Context
{
    /// <summary>
    /// Пользователь
    /// </summary>
    public class RegisterModel
    {
        /// <summary>
        /// Логин
        /// </summary>
        [Required, MinLength(1), MaxLength(256)]
        [Display(Name = "Логин")]
        public string Login { get; set; }

        /// <summary>
        /// Пароль
        /// </summary>
        [Required, MinLength(1), MaxLength(256)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }
        
        /// <summary>
        /// Пароль (повтор)
        /// </summary>
        [Required, Compare(nameof(Password))]
        [Display(Name = "Пароль (повтор)")]
        public string RepeatPassword { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        [Required, MinLength(1), MaxLength(100)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        [MaxLength(100)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        /// <summary>
        /// Возраст
        /// </summary>
        [Range(0, 255)]
        [Display(Name = "Возраст")]
        public byte Age { get; set; }

        /// <summary>
        /// Пол
        /// </summary>
        [Display(Name = "Пол")]
        public GenderTypes Gender { get; set; }

        /// <summary>
        /// Интересы
        /// </summary>
        [Display(Name = "Интересы")]
        public HobbiesKinds Hobby { get; set; }

        /// <summary>
        /// Город
        /// </summary>
        [MaxLength(100)]
        [Display(Name = "Город")]
        public string City { get; set; }
    }
}
