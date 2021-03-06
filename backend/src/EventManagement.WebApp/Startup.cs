using AutoMapper;
using EventManagement.DataAccess;
using EventManagement.Identity;
using EventManagement.WebApp.Configuration;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NSwag.AspNetCore;

namespace EventManagement.WebApp
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
            services.AddDbContext<EventsDbContext>(
                options => options.UseSqlServer(
                    Configuration.GetConnectionString("EventManagement")));
            services.AddTransient<EventsDbInitializer>();

            services.Configure<RouteOptions>(options =>
            {
                // Generated path urls should be lowercase.
                options.LowercaseUrls = true;
            });

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddApplicationPart(typeof(AccountController).Assembly)
                .AddJsonOptions(options =>
                {
                    // Important: ASP.NET Core is serializing dates to JSON as local time.
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            services.AddWebOptimizer(pipeline =>
            {
                pipeline.AddLessBundle("css/site.css", "css/site.less");
                pipeline.AddLessBundle("css/ticket-validation.css", "css/ticket-validation.less");
            });

            services.AddIdentityServer()
                .AddDeveloperSigningCredential(persistKey: true)
                .AddInMemoryApiResources(IdentityServerConfig.GetApis())
                .AddInMemoryIdentityResources(IdentityServerConfig.GetIdentityResources())
                .AddClientStore<EventManagementLocalClientStore>()
                .AddProfileService<UserProfileService>();
            services.AddTransient<IUserStore, UserStore>();

            // Configure authentication to protect our web api.
            services.AddAuthentication()
                .AddLocalApi(Constants.JwtAuthScheme, options =>
                {
                    options.ExpectedScope = "eventmanagement.admin";
                });

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

            services.AddSwaggerDocument();
            services.AddAutoMapper(GetType());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, EventsDbContext dbContext, EventsDbInitializer dbInitializer)
        {
            dbContext.Database.Migrate();
            dbInitializer.EnsureInitialData(new TestData());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseWebOptimizer();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUi3();

            app.UseIdentityServer();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    // spa.UseAngularCliServer(npmScript: "start");

                    spa.UseProxyToSpaDevelopmentServer("http://localhost:4200");
                }
            });
        }
    }
}