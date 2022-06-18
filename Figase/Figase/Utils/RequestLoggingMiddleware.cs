using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Figase.Utils
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IConfiguration config)
        {
            logger = loggerFactory.CreateLogger($"{Assembly.GetEntryAssembly().GetName().Name}.{Assembly.GetEntryAssembly().GetName().Name.Split('.').Last()}_{nameof(RequestLoggingMiddleware)}");
            this.next = next;

            logger.LogInformation($"{nameof(RequestLoggingMiddleware)} init.");
        }

        public async Task Invoke(HttpContext context)
        {
            //Запросы к хабам не логируем
            if (context.Request.Path.ToString().Contains("/hub/"))
            {
                await next(context);
                return;
            }

            // Логируем факт начала запроса
            var requestPath = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            logger.Log(LogLevel.Debug, $"BEGIN Request [{context.Request.Method}] {requestPath} (Content-Type: '{context.Request.ContentType}', Length: '{context.Request.ContentLength}') from {context.Connection.RemoteIpAddress}");
            
            await next(context);
        }
    }
}
