using UnreadMessagesCounter.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnreadMessagesCounter.Services;
using UnreadMessagesCounter.Options;

namespace UnreadMessagesCounter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddMvc();

            services.AddTransient<MySqlConnection>(_ => new MySqlConnection(Configuration.GetConnectionString("MySqlDatabase")));
            
            services.Configure<KafkaOptions>(Configuration.GetSection(KafkaOptions.Section));
            services.AddSingleton<KafkaService, KafkaService>();
            services.AddSingleton<CounterSagaService, CounterSagaService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            //app.UseStaticFiles();

            app.UseRouting();

            app.UseMiddleware<RequestLoggingMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Service}/{action=Version}");
            });

            // Прогрев сервиса-синглтона
            app.ApplicationServices.GetService(typeof(CounterSagaService));
        }
    }
}
