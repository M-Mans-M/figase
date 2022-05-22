using Figase.Enums;
using System.ComponentModel.DataAnnotations;

namespace Figase.Context
{
    /// <summary>
    /// Пост
    /// </summary>
    public class SendNewPostModel
    {
        /// <summary>
        /// Логин
        /// </summary>
        [Required, MinLength(1), MaxLength(1000)]
        [Display(Name = "Сообщение")]
        public string Message { get; set; }
    }
}
