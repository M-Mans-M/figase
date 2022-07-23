using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ChatModule
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IConfiguration configuration = null;

            return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                if (args != null) config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                configuration = context.Configuration;
                services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(context.Configuration);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.ConfigureKestrel(options =>
                {
                    var portRaw = configuration["Host:Port"];
                    int port = 0;
                    if (string.IsNullOrEmpty(portRaw) || !int.TryParse(portRaw, out port))
                        port = 5001;

                    Console.WriteLine("Listening " + port);
                    options.Listen(IPAddress.Any, port);
                });
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.ClearProviders();
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Trace);
            })
            .UseNLog();
        }            
    }
}
