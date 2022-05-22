using Figase.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Figase.Context
{
    // CREATE TABLE IF NOT EXISTS Subs(PersonId INT, SubPersonId INT, INDEX PK_Subs(PersonId, SubPersonId))

    /// <summary>
    /// Модель подписок пользователей друг к друру
    /// </summary>
    public class PersonSub
    {
        /// <summary>
        /// Идентификатор пользователя который на кого-то подписан
        /// </summary>
        public int PersonId { get; set; }

        /// <summary>
        /// Идентификатор пользователя на которого эта подписка
        /// </summary>
        public int SubPersonId { get; set; }
    }
}
