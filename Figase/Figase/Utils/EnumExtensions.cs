using System;
using System.ComponentModel;
using System.Linq;

namespace Figase.Utils
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Получить описание атрибута "Description"
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum value)
        {
            return (value.GetType().GetMember(value.ToString()).FirstOrDefault()?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute)?.Description ?? "Unknown";
        }
    }
}
