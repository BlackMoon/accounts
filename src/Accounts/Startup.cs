using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DryIoc;
using DryIoc.Dnx.DependencyInjection;
using Kit.Dal.Configurations;
using Kit.Dal.DbManager;
using Kit.Kernel.Configuration;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Job;
using Kit.Kernel.CQRS.Query;
using Kit.Kernel.Identity;
using Kit.Kernel.Interception;
using Kit.Kernel.Interception.Attribute;
using Kit.Kernel.Web.Configuration;
using Kit.Kernel.Web.EncryptData;
using Kit.Kernel.Web.Filter;
using Kit.Kernel.Web.ForceHttpsMiddleware;
using Kit.Kernel.Web.Job;
using Mapster;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace accounts
{
    public class Startup
    {
        public const string AuthenticationSchemeName = "Cookies";

        public IConfigurationRoot Configuration { get; set; }

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
            container.RegisterMany(implTypeAssemblies, (registrator, types, type) =>
            {
                // all dispatchers --> currentScope
                IReuse reuse = type.IsAssignableTo(typeof(ICommandDispatcher)) || type.IsAssignableTo(typeof(IJobDispatcher)) || type.IsAssignableTo(typeof(IQueryDispatcher))
                    ? Reuse.InCurrentScope
                    : Reuse.Transient;
                
                registrator.RegisterMany(types, type, reuse);

                // interceptors
                if (type.IsClass)
                {
                    InterceptedObjectAttribute attr = (InterceptedObjectAttribute)type.GetCustomAttribute(typeof(InterceptedObjectAttribute));
                    if (attr != null)
                    {
                        Type serviceType = attr.ServiceInterfaceType ?? type.GetImplementedInterfaces().FirstOrDefault();
                        registrator.RegisterInterfaceInterceptor(serviceType, attr.InterceptorType);
                    }
                }
            });
            
            // IDbManager
            container.RegisterDelegate(delegate(IResolver resolver)
            {
                HttpContext httpContext = resolver.Resolve<IHttpContextAccessor>().HttpContext;
                return httpContext.User.GetConnectionString();
                
            }, serviceKey: "ConnectionString");

            container.RegisterInstance(Configuration["Data:DefaultConnection:ProviderName"], serviceKey: "ProviderName");
            container.Register(
                reuse: Reuse.InWebRequest, 
                made: Made.Of(() => DbManagerFactory.CreateDbManager(Arg.Of<string>("ProviderName"), Arg.Of<string>("ConnectionString")), requestIgnored => string.Empty)
                );
            
            return container;
        }

        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("cachesettings.json", true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(true);
            }
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddOptions();

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<ConnectionStringSettings>(Configuration.GetSection("Data:DefaultConnection"));
            services.Configure<CookieAuthenticationConfiguration>(Configuration.GetSection("CookieAuthentication"));
            services.Configure<ForceHttpsOptions>(Configuration.GetSection("HttpsOptions"));
            services.Configure<OracleEnvironmentSettings>(Configuration.GetSection("OracleEnvironment"));

            services.AddDataProtection();

            services
                .AddMvc(options =>
                {
                    IModelBinder originalBinder = options.ModelBinders.FirstOrDefault(x => x.GetType() == typeof(MutableObjectModelBinder));
                    int ix = options.ModelBinders.IndexOf(originalBinder);
                    options.ModelBinders.Remove(originalBinder);
                    options.ModelBinders.Insert(ix, new EncryptModelBinder());
                })
                .AddJsonOptions(option =>
                {
                    option.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    option.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });

            // Global exceptions' filter
            services.Configure<MvcOptions>(options => options.Filters.Add(new GlobalExceptionFilter()));

            services.ConfigureRouting(
                routeOptions =>
                {
                    routeOptions.LowercaseUrls = true;
                    routeOptions.AppendTrailingSlash = true;
                });

            // Add dependencies
            IContainer container = ConfigureDependencies(services);

            // Startup Jobs
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
           
            // cookie configuration
            CookieAuthenticationOptions options = new CookieAuthenticationOptions()
            {
                AuthenticationScheme = AuthenticationSchemeName,
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                LoginPath = "/auth/login",
                SlidingExpiration = true
            };

            IOptions<CookieAuthenticationConfiguration> config = app.ApplicationServices.GetService<IOptions<CookieAuthenticationConfiguration>>();
            options = config.Value.Adapt(options);
            options.ExpireTimeSpan = TimeSpan.FromMinutes(config.Value.TimeOut);            // TimeSpan Type cant' auto map
            app.UseCookieAuthentication(options);

            // https
            if (Configuration.Get<bool>("HttpsOptions:Enabled"))
            {
                IOptions<ForceHttpsOptions> httpsOptions = app.ApplicationServices.GetService<IOptions<ForceHttpsOptions>>();
                app.UseForceHttps(httpsOptions.Value);
            }

            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute("ChangePassword", "change", new { controller = "Auth", action = "ChangePassword" });
                routes.MapRoute("Login", "login", new { controller = "Auth", action = "Login" });
                routes.MapRoute("Default", "{controller=Auth}/{action=Login}");
            });

            // Request Jobs
            IJobDispatcher dispatcher = app.ApplicationServices.GetRequiredService<IJobDispatcher>();
            dispatcher.Dispatch<IRequestJob>();
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
