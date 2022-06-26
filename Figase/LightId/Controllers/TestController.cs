using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Threading.Tasks;

namespace LightId.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IServiceProvider serviceProvider;
        private readonly int serviceId;

        public TestController(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            this.serviceProvider = serviceProvider;
            serviceId = configuration.GetValue<int>("AppSettings:ServiceId");
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("data")]
        public async Task<IActionResult> GetData()
        {
            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            await connection.OpenAsync();

            int dbInstance = 0;

            using (var command = new MySqlCommand($"show variables like 'server_id'", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        dbInstance = GetValue<int>(reader.GetValue(1));
                    }
                };
            }

            await connection.CloseAsync();

            return Ok(new
            {
                Server = serviceId,
                Database = dbInstance
            });
        }

        private T GetValue<T>(object value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
