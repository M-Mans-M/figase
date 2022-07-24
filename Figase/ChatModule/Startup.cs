using ChatModule.Extensions;
using ChatModule.Options;
using ChatModule.Utils;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatModule
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
            //services.Configure<ConsulConfig>(Configuration.GetSection("Consul"));
            //services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            //{
            //    var address = Configuration["Consul:Host"];
            //    consulConfig.Address = new Uri(address);
            //}));

            services.AddControllers();
            services.AddMvc();

            //services.AddSingleton<ZabbixRedMeticsService, ZabbixRedMeticsService>();

            services.AddTransient<MySqlConnection>(_ => new MySqlConnection(Configuration.GetConnectionString("MySqlDatabase")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime) // Microsoft.AspNetCore.Hosting.IApplicationLifetime lifetime)
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

            //app.UseHttpMetrics();
            app.UseHttpMetrics(options =>
            {
                // This will preserve only the first digit of the status code.
                // For example: 200, 201, 203 -> 2xx
                options.ReduceStatusCodeCardinality();
            });

            //app.RegisterWithConsul(lifetime);
            app.UseMiddleware<RequestLoggingMiddleware>();
            
            //app.WarmZabbixRedMeticsService();
            //app.UseMiddleware<ZabbixRedMeticsMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Service}/{action=Version}");

                endpoints.MapMetrics();
            });
        }
    }
}
