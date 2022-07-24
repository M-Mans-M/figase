using ChatModule.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace ChatModule.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServiceController : Controller
    {
        /// [GET] /service/version
        /// <summary>
        /// Служебная информация о микросервисе
        /// </summary>
        /// <remarks>
        /// Возвращает номер версии API и прочую служебную информацию
        /// </remarks>
        /// <response code="200">Ошибок нет</response>        
        [HttpGet]
        [Route("version")]
        [AllowAnonymous]
        public IActionResult Version()
        {
            return Ok(new ApiVersionResponseModel());
        }

        [HttpGet]
        [Route("test_error")]
        [AllowAnonymous]
        public IActionResult Test_Error()
        {
            return StatusCode(500);
        }

        [HttpGet]
        [Route("test_delay")]
        [AllowAnonymous]
        public IActionResult Test_Delay()
        {
            Thread.Sleep(100);
            return StatusCode(200);
        }
    }
}
