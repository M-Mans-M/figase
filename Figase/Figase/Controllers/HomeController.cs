using Figase.Context;
using Figase.Enums;
using Figase.Models;
using Figase.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Figase.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly IServiceProvider serviceProvider;

        public HomeController(ILogger<HomeController> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userIdRaw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (Int32.TryParse(userIdRaw, out var userId))
            {
                var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
                await connection.OpenAsync();

                Person person = null;
                using (var command = new MySqlCommand($"SELECT * FROM Persons WHERE Id = {userId} LIMIT 1", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            person ??= fetchPerson(reader);
                        }
                    };
                }

                return View(person);
            }

            return View(null);
        }

        /// <summary>
        /// Переход на страницу авторизации
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            var model = new LoginModel();
            return View(model);
        }

        /// <summary>
        /// Обработка запроса авторизации
        /// </summary>
        /// <param name="model">Модель авторизации</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
                await connection.OpenAsync();

                Person person = null;
                using (var command = new MySqlCommand($"SELECT * FROM Persons WHERE Login LIKE '{model.Login}' LIMIT 1", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            person ??= fetchPerson(reader);
                        }
                    }
                }

                if (person == null)
                {
                    ViewBag.Message = "Пользователь не найден";
                    return View(model);
                }

                var password = new HashProvider().ComputeHash(model.Login.ToLower() + model.Password);
                if (person.Password != password)
                {
                    ViewBag.Message = "Неправильные логин или пароль";
                    return View(model);
                }

                var claims = new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, Convert.ToString(person.Id)),
                        new Claim(ClaimTypes.Name, person.FirstName),
                        new Claim(ClaimTypes.Role, "User")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties() { IsPersistent = true });
                return LocalRedirect("/");
            }
            return View(model);
        }

        /// <summary>
        /// Выход из системы
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> LogOut()
        {
            // Деавторизация    
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Переход на страницу регистрации
        /// </summary>
        /// <returns></returns>
        public IActionResult Register()
        {
            var model = new RegisterModel();
            return View(model);
        }

        /// <summary>
        /// Обработка запроса авторизации
        /// </summary>
        /// <param name="model">Модель авторизации</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
                await connection.OpenAsync();

                Person person = null;
                using (var command = new MySqlCommand($"SELECT * FROM Persons WHERE Login LIKE '{model.Login}' LIMIT 1", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            person ??= fetchPerson(reader);
                        }
                    }
                }

                if (person != null)
                {
                    ViewBag.Message = "Логин уже занят";
                    return View(model);
                }

                // Создание пользователя
                var password = new HashProvider().ComputeHash(model.Login.ToLower() + model.Password);
                person = new Person
                {
                    Login = model.Login,
                    Password = password,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Age = model.Age,
                    Gender = model.Gender,
                    Hobby = model.Hobby,
                    City = model.City
                };

                using (var command = new MySqlCommand($"INSERT INTO Persons (Login, Password, FirstName, LastName, Age, Gender, Hobby, City) VALUES ('{person.Login}','{person.Password}','{person.FirstName}','{person.LastName}',{person.Age},{(int)person.Gender},{(int)person.Hobby},'{person.City}')", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            person ??= fetchPerson(reader);
                        }
                    }
                }

                // Авторизация
                var claims = new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, Convert.ToString(person.Id)),
                        new Claim(ClaimTypes.Name, person.FirstName),
                        new Claim(ClaimTypes.Role, "User")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties() { IsPersistent = true });
                return LocalRedirect("/");
            }
            return View(model);
        }

        /// <summary>
        /// Переход на страницу регистрации
        /// </summary>
        /// <returns></returns>
        public IActionResult Search()
        {
            var model = new SearchModel();
            return View(model);
        }

        /// <summary>
        /// Обработка запроса авторизации
        /// </summary>
        /// <param name="model">Модель авторизации</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Search(SearchModel model)
        {
            if (ModelState.IsValid)
            {
                var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
                await connection.OpenAsync();

                var whereClauses = new List<string>();
                if (!string.IsNullOrEmpty(model.FirstNamePrefix)) whereClauses.Add($"FirstName LIKE '{model.FirstNamePrefix}%'");
                if (!string.IsNullOrEmpty(model.LastNamePrefix)) whereClauses.Add($"LastName LIKE '{model.FirstNamePrefix}%'");
                if (model.PageNum <= 0) model.PageNum = 1;
                if (model.PageSize <= 0) model.PageSize = 30;

                var cmdSb = new StringBuilder("SELECT * FROM Persons");
                if (whereClauses.Count > 0)
                {
                    cmdSb.Append($" WHERE");
                    cmdSb.Append($" {string.Join(" AND ", whereClauses)}");
                }
                cmdSb.Append($" LIMIT {model.PageSize}");
                cmdSb.Append($" OFFSET {(model.PageNum - 1) * model.PageSize}");

                List<Person> persons = null;
                using (var command = new MySqlCommand(cmdSb.ToString(), connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            persons ??= new List<Person>();

                            var person = fetchPerson(reader);

                            persons.Add(person);
                        }
                    }
                }

                model.Result = persons;
            }
            return View("Search", model);
        }

        /// <summary>
        /// Обработка запроса авторизации
        /// </summary>
        /// <param name="model">Модель авторизации</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ApiSearch(SearchModel model)
        {
            if (ModelState.IsValid)
            {
                var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
                await connection.OpenAsync();

                var whereClauses = new List<string>();
                if (!string.IsNullOrEmpty(model.FirstNamePrefix)) whereClauses.Add($"FirstName LIKE '{model.FirstNamePrefix}%'");
                if (!string.IsNullOrEmpty(model.LastNamePrefix)) whereClauses.Add($"LastName LIKE '{model.FirstNamePrefix}%'");
                if (model.PageNum <= 0) model.PageNum = 1;
                if (model.PageSize <= 0) model.PageSize = 30;

                var cmdSb = new StringBuilder("SELECT * FROM Persons");
                if (whereClauses.Count > 0)
                {
                    cmdSb.Append($" WHERE");
                    cmdSb.Append($" {string.Join(" AND ", whereClauses)}");
                }
                cmdSb.Append($" LIMIT {model.PageSize}");
                cmdSb.Append($" OFFSET {(model.PageNum - 1) * model.PageSize}");

                List<Person> persons = null;
                using (var command = new MySqlCommand(cmdSb.ToString(), connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            persons ??= new List<Person>();

                            var person = fetchPerson(reader);

                            persons.Add(person);
                        }
                    }
                }

                return Ok(persons);
            }
            return BadRequest();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private Person fetchPerson(MySqlDataReader reader)
        {
            var person = new Person();
            person.Id = (int)reader.GetValue(0);
            person.Login = reader.GetValue(1) as string;
            person.Password = reader.GetValue(2) as string;
            person.FirstName = reader.GetValue(3) as string;
            person.LastName = reader.GetValue(4) as string;
            person.Age = (byte)reader.GetValue(5);
            person.Gender = (GenderTypes)(int)reader.GetValue(6);
            person.Hobby = (HobbiesKinds)(int)reader.GetValue(7);
            person.City = reader.GetValue(8) as string;

            return person;
        }
    }
}
