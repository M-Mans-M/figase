using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ZabbixAPICore;

namespace ChatModule.Utils
{
    public class ZabbixRedMeticsMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly ZabbixRedMeticsService zabbixRedMeticsService;

        public ZabbixRedMeticsMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IConfiguration config, IServiceProvider serviceProvider)
        {
            logger = loggerFactory.CreateLogger($"{Assembly.GetEntryAssembly().GetName().Name}.{Assembly.GetEntryAssembly().GetName().Name.Split('.').Last()}_{nameof(RequestLoggingMiddleware)}");
            this.next = next;

            zabbixRedMeticsService = serviceProvider.GetService<ZabbixRedMeticsService>();

            logger.LogInformation($"{nameof(ZabbixRedMeticsMiddleware)} init.");
        }

        public async Task Invoke(HttpContext context)
        {
            //Запросы к хабам не считаем
            if (context.Request.Path.ToString().Contains("/hub/"))
            {
                await next(context);
                return;
            }

            zabbixRedMeticsService?.OnRequetsBegin();
            var sw = Stopwatch.StartNew();

            await next(context);

            sw.Stop();
            zabbixRedMeticsService?.OnRequetsEnd(context.Response.StatusCode >= 500, sw.ElapsedMilliseconds);
        }
    }

    public class ZabbixRedMeticsService
    {
        //
        // RED
        // Rate: the number of requests our service is serving per second;
        // Error: the number of failed requests per second;
        // Duration: the amount of time it takes to process a request;
        //

        private readonly Timer timer;
        private readonly IConfiguration config;

        private int requestsCount = 0;
        private int errorsCount = 0;
        private int requestsDuration = 0;

        //private readonly Zabbix zabbix = new Zabbix("admin", "zabbix", "http://192.168.27.42:8080/api_jsonrpc.php");
        //private readonly ZabbixSender.Async.Sender zabbix = new ZabbixSender.Async.Sender("192.168.27.42");

        private readonly string host;

        public ZabbixRedMeticsService(IConfiguration config)
        {
            this.config = config;
            host = "Chat-" + config["Host:Port"];
            timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        private bool sendZabbix(string key, string value)
        {
            var zabbix = new ZabbixSender.Async.Sender("192.168.27.42");
            try
            {
                var response = zabbix.Send(host, key, value).GetAwaiter().GetResult();
                return response.IsSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during sending data [{host}, {key}, {value}] to zabbix: {ex}");
            }
            return false;
        }

        private void OnTimerTick(object state)
        {
            var metrics = (requestsCount, errorsCount, requestsDuration);

            Interlocked.Exchange(ref requestsCount, 0);
            Interlocked.Exchange(ref errorsCount, 0);
            Interlocked.Exchange(ref requestsDuration, 0);

            sendZabbix("chat.request.count", metrics.requestsCount.ToString());
            sendZabbix("chat.errors.count", metrics.errorsCount.ToString());
            sendZabbix("chat.request.duration", metrics.requestsDuration.ToString());

            /*
            zabbix.LoginAsync().Wait();

            Task<Response> createUserResponse = zabbix.GetResponseObjectAsync("item.create", new
            {
                name = "Requests count",
                key_ = "otus.chat.requests" //"vfs.fs.size[/home/joe/,free]",
                hostid = "10084",
                type = 0,
                value_type = 3,
                interfaceid = "1",
                delay = 30
            });

            if (createUserResponse.Result.error != null)
            {
                var error = createUserResponse.Result.error;
                Console.WriteLine(error.GetErrorMessage());
            }
            else
            {
                userid = createUserResponse.Result.result.userids[0];
                Console.WriteLine(userid);
            }
            */
        }

        public void OnRequetsBegin()
        {
            Interlocked.Increment(ref requestsCount);
        }

        public void OnRequetsEnd(bool isError, double duration)
        {
            Interlocked.Add(ref requestsDuration, (int)duration);

            if (isError)
                Interlocked.Increment(ref errorsCount);
        }
    }

    public static class ZabbixRedMeticsServiceExtensions
    {
        public static IApplicationBuilder WarmZabbixRedMeticsService(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<ZabbixRedMeticsService>();
            return app;
        }
    }
}
