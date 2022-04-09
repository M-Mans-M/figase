using Figase.Enums;
using System.ComponentModel.DataAnnotations;

namespace Figase.Context
{
    /// <summary>
    /// Пользователь
    /// </summary>
    public class Person
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Логин
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Пароль
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Возраст
        /// </summary>
        public byte Age { get; set; }

        /// <summary>
        /// Пол
        /// </summary>
        public GenderTypes Gender { get; set; }

        /// <summary>
        /// Интересы
        /// </summary>
        public HobbiesKinds Hobby { get; set; }

        /// <summary>
        /// Город
        /// </summary>
        public string City { get; set; }
    }
}
