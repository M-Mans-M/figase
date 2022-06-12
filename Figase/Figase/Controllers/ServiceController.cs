using Figase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Figase.Controllers
{
    public class ServiceController : Controller
    {
        /// [GET] /v1/service/version
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
