using Figase.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Figase.Context
{
    // CREATE TABLE IF NOT EXISTS Posts(Id INT NOT NULL AUTO_INCREMENT, PersonId INT NOT NULL, Created DATETIME NOT NULL, Content TEXT NOT NULL, PRIMARY KEY (Id));

    /// <summary>
    /// Пост пользователя в личном профиле (а-ля блоге)
    /// </summary>
    public class PersonPost
    {
        /// <summary>
        /// Идентификатор сообщения
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public int PersonId { get; set; }

        /// <summary>
        /// Дата и время создания
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Содержимое поста
        /// </summary>
        public string Content { get; set; }
    }
}
