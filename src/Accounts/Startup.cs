using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DryIoc;
using DryIoc.Dnx.DependencyInjection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kit.Dal.Configurations;
using Kit.Dal.DbManager;
using Kit.Kernel.Configuration;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Job;
using Kit.Kernel.CQRS.Query;
using Kit.Kernel.Interception;
using Microsoft.AspNet.Authentication.Cookies;

namespace accounts
{
    public class Startup
    {
        private IContainer ConfigureDependencies(IServiceCollection services)
        {
            IEnumerable<string> implAssembliesNames = new[] { "Kit.Dal" , "Kit.Kernel" };

            // Register Kit.Kernel assembly
            IEnumerable<AssemblyName> assemblyNames = Assembly.GetExecutingAssembly()
                .GetReferencedAssemblies()
                .Where(a => implAssembliesNames.Contains(a.Name))
                .ToList();

            IList<Assembly> implTypeAssemblies = new List<Assembly>(assemblyNames.Count());
            foreach (AssemblyName an in assemblyNames)
            {
                implTypeAssemblies.Add(Assembly.Load(an));
            }
            
            IContainer container = new Container().WithDependencyInjectionAdapter(services);
            container.RegisterMany(implTypeAssemblies);
            
            // dispatchers
            container.Register<ICommandDispatcher, CommandDispatcher>(Reuse.InCurrentScope, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
            container.Register<IJobDispatcher, JobDispatcher>(Reuse.InCurrentScope, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
            container.Register<IQueryDispatcher, QueryDispatcher>(Reuse.InCurrentScope, ifAlreadyRegistered: IfAlreadyRegistered.Replace);

            container.RegisterInterfaceInterceptor<ICommandDispatcher, FooLoggingInterceptor>();
            // IDbManager
            container.RegisterInstance(Configuration["Data:DefaultConnection:ProviderName"], serviceKey: "ProviderName");
            container.Register(
                reuse: Reuse.InWebRequest, 
                made: Made.Of(() => DbManagerFactory.CreateDbManager(Arg.Of<string>("ProviderName")), requestIgnored => string.Empty)
                );
            
            return container;
        }

        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(true);
            }
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc();

            services.ConfigureRouting(
                routeOptions =>
                {
                    routeOptions.LowercaseUrls = true;
                    routeOptions.AppendTrailingSlash = true;
                });

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<ConnectionStringSettings>(Configuration.GetSection("Data:DefaultConnection"));
            services.Configure<OracleEnvironmentSettings>(Configuration.GetSection("OracleEnvironment"));

            // Add dependencies
            IContainer container = ConfigureDependencies(services);

            // Startup Tasks
            IJobDispatcher dispatcher = container.Resolve<IJobDispatcher>();
            dispatcher.Dispatch<IStartupJob>();

            return container.Resolve<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            
            app.UseApplicationInsightsRequestTelemetry();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
                app.UseExceptionHandler("/Auth/Error");
            
            app.UseIISPlatformHandler();
            app.UseApplicationInsightsExceptionTelemetry();

            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                AuthenticationScheme = "Cookies",
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                //o.CookieName = "NTC." + Path.GetRandomFileName();
                LoginPath = "/auth/login",
                ExpireTimeSpan = TimeSpan.FromMinutes(20),
                SlidingExpiration = true
            });
            
            //app.UseForceHttps(new ForceHttpsOptions() { SecurePort = 44354, Paths = new []{"/auth/index"}});
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute("ChangePassword", "change", new { controller = "Auth", action = "ChangePassword" });
                routes.MapRoute("Login", "login", new { controller = "Auth", action = "Login" });
                routes.MapRoute("Default", "{controller=Auth}/{action=Login}");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
