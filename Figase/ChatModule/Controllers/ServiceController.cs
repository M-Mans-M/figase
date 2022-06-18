using ChatModule.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}
