using Figase.Hubs;
using Figase.Options;
using Figase.Services;
using Figase.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;

namespace Figase
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
            services.AddControllersWithViews();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(x => x.LoginPath = "/Home/Login");

            // Other configuration code
            services.AddMvc(options =>
            {
                options.ModelBinderProviders.Insert(0, new FlagsEnumModelBinderProvider());
            });

            //services.AddDbContext<MySqlContext>(options =>
            //    options.UseMySQL(Configuration.GetConnectionString("MySqlDatabase")));

            services.AddTransient<MySqlConnection>(_ => new MySqlConnection(Configuration.GetConnectionString("MySqlDatabase")));

            services.Configure<KafkaOptions>(Configuration.GetSection(KafkaOptions.Section));
            services.AddSingleton<KafkaService, KafkaService>();
            services.AddSingleton<MainService, MainService>();

            services.AddSignalR();
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
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();
                        
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Search}/{id?}");
                endpoints.MapHub<NewsHub>("/newsHub");
            });
        }
    }
}
