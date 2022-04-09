using Figase.Context;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Figase.Models
{
    public class SearchModel
    {
        /// <summary>
        /// Префикс имени
        /// </summary>
        [MaxLength(256)]
        [Display(Name = "Имя")]
        public string FirstNamePrefix { get; set; }

        /// <summary>
        /// Префикс фамилии
        /// </summary>
        [MaxLength(256)]
        [Display(Name = "Фамилия")]
        public string LastNamePrefix { get; set; }

        /// <summary>
        /// Максимальное количество записей
        /// </summary>
        public int PageSize { get; set; } = 30;

        /// <summary>
        /// Номер страницы
        /// </summary>
        public int PageNum { get; set; } = 1;

        /// <summary>
        /// Результат поиска
        /// </summary>
        public List<Person> Result { get; set; }
    }
}
