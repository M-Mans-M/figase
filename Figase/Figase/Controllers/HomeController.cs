using Figase.Context;
using Figase.Enums;
using Figase.Hubs;
using Figase.Models;
using Figase.Services;
using Figase.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly KafkaService kafkaService;
        private readonly MainService mainService;

        public HomeController(ILogger<HomeController> logger, IServiceProvider serviceProvider, MainService mainService, KafkaService kafkaService)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.kafkaService = kafkaService;
            this.mainService = mainService;
        }

        [Authorize]
        [Obsolete("Не используется")]
        public async Task<IActionResult> Index()
        {
            var userIdRaw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (Int32.TryParse(userIdRaw, out var userId))
            {
                var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
                await connection.OpenAsync();

                Person person = null;
                using (var command = new MySqlCommand($"SELECT * FROM persons WHERE Id = {userId} LIMIT 1", connection))
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
                using (var command = new MySqlCommand($"SELECT * FROM persons WHERE Login LIKE '{model.Login}' LIMIT 1", connection))
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

                mainService.subsCache[person.Id] = new Dictionary<int, List<PersonPost>>();
                
                //Первиичная загрузка кеша постов пользователей н акоторых есть подписка                
                var mySubs = new List<int>(); // Пользователи на которых я подписан
                using (var command = new MySqlCommand($"SELECT SubPersonId FROM subs WHERE PersonId = {person.Id}", connection))
                using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        mySubs.Add((int)reader.GetValue(0));

                foreach (var sub in mySubs) mainService.subsCache[person.Id].Add(sub, new List<PersonPost>());

                // Одноразовая инициализация кеша при авторизации (дальше кеш будет обновляться реакцией на посты в кафке)
                if (mySubs.Count > 0)
                {
                    var mySubsPosts = new List<PersonPost>();
                    using (var command = new MySqlCommand($"SELECT * FROM posts WHERE PersonId IN ({string.Join(", ", mySubs)}) LIMIT 100", connection))
                    using (var reader = await command.ExecuteReaderAsync())
                        while (await reader.ReadAsync())
                            mySubsPosts.Add(fetchPost(reader));

                    foreach (var postsGroupedByPersonId in mySubsPosts.GroupBy(p => p.PersonId))                    
                        mainService.subsCache[person.Id][postsGroupedByPersonId.Key].AddRange(postsGroupedByPersonId);                    
                }

                var claims = new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, Convert.ToString(person.Id)),
                        new Claim(ClaimTypes.Name, person.FirstName),
                        new Claim(ClaimTypes.Role, "User")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await connection.CloseAsync();
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties() { IsPersistent = true });
                return LocalRedirect("/");
            }
            return View(model);
        }

        /// <summary>
        /// Выход из системы
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            var userIdRaw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userId = Int32.Parse(userIdRaw);

            // Деавторизация    
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            mainService.subsCache.Remove(userId);

            return RedirectToAction("Search");
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
                using (var command = new MySqlCommand($"SELECT * FROM persons WHERE Login LIKE '{model.Login}' LIMIT 1", connection))
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

                using (var command = new MySqlCommand($"INSERT INTO persons (Login, Password, FirstName, LastName, Age, Gender, Hobby, City) VALUES ('{person.Login}','{person.Password}','{person.FirstName}','{person.LastName}',{person.Age},{(int)person.Gender},{(int)person.Hobby},'{person.City}')", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            person ??= fetchPerson(reader);
                        }
                    }
                }

                mainService.subsCache[person.Id] = new Dictionary<int, List<PersonPost>>();

                // Авторизация
                var claims = new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, Convert.ToString(person.Id)),
                        new Claim(ClaimTypes.Name, person.FirstName),
                        new Claim(ClaimTypes.Role, "User")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await connection.CloseAsync();
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties() { IsPersistent = true });
                return LocalRedirect("~/");
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

                var cmdSb = new StringBuilder("SELECT * FROM persons");
                if (whereClauses.Count > 0)
                {
                    cmdSb.Append($" WHERE");
                    cmdSb.Append($" {string.Join(" AND ", whereClauses)}");
                }
                cmdSb.Append($" LIMIT {model.PageSize}");
                cmdSb.Append($" OFFSET {(model.PageNum - 1) * model.PageSize}");

                var userIdRaw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var userId = 0;
                Int32.TryParse(userIdRaw, out userId);

                // Найти всех на кого подписан этот пользователь
                //var usersSubbedToMe = new List<int>();
                //using (var command = new MySqlCommand($"SELECT * FROM Subs WHERE PersonId = {userId}", connection))
                //using (var reader = await command.ExecuteReaderAsync())
                //    while (await reader.ReadAsync())
                //        usersSubbedToMe.Add((int)reader.GetValue(1));
                //mainService.subsCache[userId] = usersSubbedToMe;

                List<PersonViewModel> personsViewModels = null;
                using (var command = new MySqlCommand(cmdSb.ToString(), connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            personsViewModels ??= new List<PersonViewModel>();
                            var person = fetchPerson(reader);
                                                        
                            var viewModel = new PersonViewModel(person);
                            if (userId != 0 && mainService.subsCache.TryGetValue(userId, out var subsList))
                                viewModel.Subscribed = subsList.ContainsKey(viewModel.Id);

                            personsViewModels.Add(viewModel);
                        }
                    }
                }

                await connection.CloseAsync();
                model.Result = personsViewModels;
            }
            return View("Search", model);
        }

        /// <summary>
        /// Обработка запроса авторизации
        /// </summary>
        /// <param name="model">Модель авторизации</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ApiSearch([FromBody] SearchModel model)
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

                var cmdSb = new StringBuilder("SELECT * FROM persons");
                if (whereClauses.Count > 0)
                {
                    cmdSb.Append($" WHERE");
                    cmdSb.Append($" {string.Join(" AND ", whereClauses)}");
                }
                //cmdSb.Append($" LIMIT {model.PageSize}");
                //cmdSb.Append($" OFFSET {(model.PageNum - 1) * model.PageSize}");

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

                await connection.CloseAsync();
                return Ok(persons);
            }
            return BadRequest();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Показать профиль пользователя
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> Profile(int? id = null)
        {
            int userId = 0;
            if (id == null)
            {
                var userIdRaw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                userId = Int32.Parse(userIdRaw);
            }
            else
                userId = id.Value;

            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            await connection.OpenAsync();

            Person person = null;
            using (var command = new MySqlCommand($"SELECT * FROM persons WHERE Id = {userId} LIMIT 1", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        person ??= fetchPerson(reader);
                    }
                };
            }

            var result = new PersonViewModel(person);

            var posts = new List<PersonPost>();
            using (var command = new MySqlCommand($"SELECT * FROM posts WHERE PersonId = {userId} ORDER BY Created DESC LIMIT 10;", connection))
            using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    posts.Add(fetchPost(reader));
            result.Posts = posts;

            if (mainService.subsCache.ContainsKey(userId))
            {
                result.SubPosts = mainService.subsCache[userId].SelectMany(c => c.Value).OrderByDescending(p => p.Created).ToList();
            }

            await connection.CloseAsync();
            return View(result);
        }

        

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Subscribe(int subPersonId)
        {
            if (subPersonId == 0) return BadRequest();

            var userIdRaw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userId = Int32.Parse(userIdRaw);

            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            await connection.OpenAsync();

            // Поищем дубликат
            int count = 0;
            using (var command = new MySqlCommand($"SELECT COUNT(*) FROM subs WHERE PersonId = {userId} AND SubPersonId = {subPersonId} LIMIT 1", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        count = GetValue<int>(reader.GetValue(0));
                    }
                }
            }

            if (count != 0)
                return Conflict();

            using (var command = new MySqlCommand($"INSERT INTO subs (PersonId, SubPersonId) VALUES ({userId},{subPersonId})", connection))
            {
                await command.ExecuteReaderAsync();
            }

            mainService.subsCache[userId].Add(subPersonId, new List<PersonPost>());
            await connection.CloseAsync();

            await createPostAsync(userId, $"Подписался на пользователя {subPersonId}");
            return Ok("");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Unsubscribe(int subPersonId)
        {
            if (subPersonId == 0) return BadRequest();

            var userIdRaw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userId = Int32.Parse(userIdRaw);

            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            await connection.OpenAsync();

            // Поищем дубликат
            int count = 0;
            using (var command = new MySqlCommand($"SELECT COUNT(*) FROM subs WHERE PersonId = {userId} AND SubPersonId = {subPersonId} LIMIT 1", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        count = GetValue<int>(reader.GetValue(0));
                    }
                }
            }

            if (count == 0)
                return Conflict();

            using (var command = new MySqlCommand($"DELETE FROM subs WHERE PersonId = {userId} AND SubPersonId = {subPersonId}", connection))
            {
                await command.ExecuteReaderAsync();
            }

            mainService.subsCache[userId].Remove(subPersonId);

            await connection.CloseAsync();

            await createPostAsync(userId, $"Отписался от пользователя {subPersonId}");
            return Ok("");
        }

        private async Task<int> createPostAsync(int personId, string content)
        {
            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();

            var post = new PersonPost
            {
                Content = content,
                PersonId = personId,
                Created = DateTime.Now
            };

            int addedId = 0;

            MySqlTransaction tran = null;
            try
            {
                tran = connection.BeginTransaction();

                using (var command = new MySqlCommand($"INSERT INTO posts(PersonId, Created, Content) VALUES({post.PersonId}, '{post.Created.ToString("yyyy-MM-dd HH:mm:ss")}', '{post.Content.Replace("'", "''")}');", connection, tran))
                using (var reader = await command.ExecuteReaderAsync())
                    await reader.ReadAsync();

                using (var command = new MySqlCommand($"SELECT LAST_INSERT_ID();", connection, tran))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            addedId = GetValue<int>(reader.GetValue(0));
                        }
                    }
                }

                tran.Commit();
            }
            catch (Exception)
            {
                tran?.Rollback();
            }

            await connection.CloseAsync();

            post.Id = addedId;
            await kafkaService.ProduceAsync(mainService.newPostTopic, Newtonsoft.Json.JsonConvert.SerializeObject(post));

            return addedId;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendNewPost(SendNewPostModel model)
        {
            if (string.IsNullOrEmpty(model?.Message)) return BadRequest();

            var userIdRaw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userId = Int32.Parse(userIdRaw);

            await createPostAsync(userId, model.Message);
            return Ok();
        }

        #region DatabaseMapping

        /// <summary>
        /// Маппинг прочитанной строки из БД в модель пользователя
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private Person fetchPerson(MySqlDataReader reader)
        {
            var person = new Person();
            person.Id = GetValue<int>(reader.GetValue(0));
            person.Login = GetValue<string>(reader.GetValue(1));
            person.Password = GetValue<string>(reader.GetValue(2));
            person.FirstName = GetValue<string>(reader.GetValue(3));
            person.LastName = GetValue<string>(reader.GetValue(4));
            person.Age = GetValue<byte>(reader.GetValue(5));
            person.Gender = (GenderTypes)GetValue<int>(reader.GetValue(6));
            person.Hobby = (HobbiesKinds)GetValue<int>(reader.GetValue(7));
            person.City = GetValue<string>(reader.GetValue(8));

            return person;
        }

        private PersonSub fetchSubs(MySqlDataReader reader)
        {
            var sub = new PersonSub();
            sub.PersonId = GetValue<int>(reader.GetValue(0));
            sub.SubPersonId = GetValue<int>(reader.GetValue(1));

            return sub;
        }

        private PersonPost fetchPost(MySqlDataReader reader)
        {
            var sub = new PersonPost();
            sub.Id = GetValue<int>(reader.GetValue(0));
            sub.PersonId = GetValue<int>(reader.GetValue(1));
            sub.Created = GetValue<DateTime>(reader.GetValue(2));
            sub.Content = GetValue<string>(reader.GetValue(3));

            return sub;
        }

        private T GetValue<T>(object value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }

        #endregion DatabaseMapping
    }
}
