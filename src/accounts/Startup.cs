using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using accounts.Configuration;
using accounts.UI;
using DryIoc;
using DryIoc.Dnx.DependencyInjection;
using IdentityServer4.Core.Configuration;
using IdentityServer4.Core.Models;
using IdentityServer4.Core.Services;
using IdentityServer4.Core.Services.InMemory;
using IdentityServer4.Core.Validation;
using Kit.Dal.Configurations;
using Kit.Dal.DbManager;
using Kit.Kernel.Configuration;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Job;
using Kit.Kernel.CQRS.Query;
using Kit.Kernel.Identity;
using Kit.Kernel.Interception;
using Kit.Kernel.Interception.Attribute;
using Kit.Kernel.Web.EncryptData;
using Kit.Kernel.Web.Mvc.Filter;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace accounts
{
    public class Startup
    {
        private readonly IApplicationEnvironment _appEnv;
        private IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment hostEnv, IApplicationEnvironment appEnv)
        {
            _appEnv = appEnv;

            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("clients.json", true)
                .AddEnvironmentVariables();

            if (hostEnv.IsDevelopment())
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(true);
            }
            Configuration = builder.Build();
        }

        private IContainer ConfigureDependencies(IServiceCollection services)
        {
            IEnumerable<string> implAssembliesNames = new[] { "Kit.Dal", "Kit.Kernel" };

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
                // all dispatchers --> Reuse.InCurrentScope
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
            container.RegisterDelegate(delegate (IResolver resolver)
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


        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<ConnectionStringSettings>(Configuration.GetSection("Data:DefaultConnection"));
            services.Configure<OracleEnvironmentSettings>(Configuration.GetSection("OracleEnvironment"));
            services.Configure<List<Client>>(Configuration.GetSection("Clients"));

            // identityServer 
            IIdentityServerBuilder builder = services.AddIdentityServer(options =>
            {
                int seconds;
                if (int.TryParse(Configuration["ExpireTime"], out seconds))
                {
                    options.AuthenticationOptions = new AuthenticationOptions()
                    {
                        CookieAuthenticationOptions =
                            new CookieAuthenticationOptions()
                            {
                                ExpireTimeSpan = TimeSpan.FromSeconds(seconds)
                            }
                    };
                }

                options.SigningCertificate = new X509Certificate2(Path.Combine(_appEnv.ApplicationBasePath, "idsrv4test.pfx"), "idsrv3test");
            });
            
            #region clients
            builder.Services.AddSingleton<IEnumerable<Client>>(provider => provider.GetService<Microsoft.Extensions.OptionsModel.IOptions<List<Client>>>().Value); 
            builder.Services.AddTransient<IClientStore, InMemoryClientStore>();
            builder.Services.AddTransient<ICorsPolicyService, InMemoryCorsPolicyService>();
            #endregion
            
            #region scopes
            builder.AddInMemoryScopes(Scopes.Get());
            #endregion

            #region users --> empty list
            builder.Services.AddInstance(new List<InMemoryUser>());
            builder.Services.AddTransient<IProfileService, Services.ProfileService>();
            builder.Services.AddTransient<IResourceOwnerPasswordValidator, InMemoryResourceOwnerPasswordValidator>();
            #endregion
           
            // for the UI
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
                })
                .AddRazorOptions(razor =>
                {
                    razor.ViewLocationExpanders.Add(new CustomViewLocationExpander());
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

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Verbose);
            loggerFactory.AddDebug(LogLevel.Verbose);
            
            app.UseDeveloperExceptionPage();
            app.UseIISPlatformHandler();

            app.UseIdentityServer();

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
