using Consul;
using Figase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Figase.Controllers
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

        /*
        [HttpGet]
        [Route("consul")]
        [AllowAnonymous]
        public IActionResult GetConsul()
        {
            List<Uri> serverUrls = new List<Uri>();

            var consulClient = new ConsulClient(c => c.Address = new Uri("http://192.168.27.42:8500"));
            var services = consulClient.Agent.Services().Result.Response;
            foreach (var service in services)
            {
                var isFigaseChat = service.Value.Tags.Any(t => t == "Figase") &&
                                  service.Value.Tags.Any(t => t == "Chat");
                if (isFigaseChat)
                {
                    var serviceUri = new Uri($"{service.Value.Address}:{service.Value.Port}");
                    serverUrls.Add(serviceUri);
                }
            }

            return Ok(serverUrls);
        }
        */
    }
}
