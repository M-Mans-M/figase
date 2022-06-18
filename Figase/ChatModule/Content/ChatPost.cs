using System;

namespace ChatModule.Content
{
    /**
     * CREATE TABLE chatposts (
          Id INT NOT NULL AUTO_INCREMENT,
          FromUserId INT NOT NULL,
          ToUserId INT NOT NULL,
          Created DATETIME NOT NULL,
          Content TEXT NOT NULL,
          primary key (Id)
        );

     */

    /// <summary>
    /// Пост пользователя в личном профиле (а-ля блоге)
    /// </summary>
    public class ChatPost
    {
        /// <summary>
        /// Идентификатор сообщения
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор пользователя отравителя
        /// </summary>
        public int FromUserId { get; set; }

        /// <summary>
        /// Идентификатор пользователя получателя
        /// </summary>
        public int ToUserId { get; set; }

        /// <summary>
        /// Дата и время создания
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Содержимое 
        /// </summary>
        public string Content { get; set; }
    }
}
