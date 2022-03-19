using Figase.Context;
using Figase.Models;
using Figase.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
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
                var dbContext = serviceProvider.GetService(typeof(MySqlContext)) as MySqlContext;
                dbContext.Database.EnsureCreated();

                var person = await dbContext.Persons.FirstOrDefaultAsync(p => p.Id == userId);
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
                var dbContext = serviceProvider.GetService(typeof(MySqlContext)) as MySqlContext;
                dbContext.Database.EnsureCreated();

                var person = await dbContext.Persons.FirstOrDefaultAsync(p => p.Login.ToLower() == model.Login.ToLower());
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
                var dbContext = serviceProvider.GetService(typeof(MySqlContext)) as MySqlContext;
                
                // Проверка существования пользователя
                var person = await dbContext.Persons.FirstOrDefaultAsync(p => p.Login.ToLower() == model.Login.ToLower());
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

                await dbContext.Persons.AddAsync(person);
                await dbContext.SaveChangesAsync();

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
