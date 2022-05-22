using Figase.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Figase.Context
{
    /// <summary>
    /// Пользователь
    /// </summary>
    public class PersonViewModel : Person
    {
        public PersonViewModel(Person p)
        {
            Id = p.Id;
            FirstName = p.FirstName;
            LastName = p.LastName;
            Age = p.Age;
            Gender = p.Gender;
            Hobby = p.Hobby;
            City = p.City;
        }

        public bool Subscribed { get; set; }

        public List<PersonPost> Posts { get; set; }
        public List<PersonPost> SubPosts { get; set; }
    }
}
