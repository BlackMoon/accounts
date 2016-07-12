using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using accounts.Configuration;
using accounts.UI;
using DryIoc;
using DryIoc.AspNetCore.DependencyInjection;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Services.InMemory;
using IdentityServer4.Validation;
using Kit.Dal.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kit.Dal.DbManager;
using Kit.Kernel.Configuration;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Job;
using Kit.Kernel.CQRS.Query;
using Kit.Kernel.Identity;
using Kit.Kernel.Interception;
using Kit.Kernel.Interception.Attribute;
using Kit.Kernel.Web.Binders;
using Kit.Kernel.Web.Mvc.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using AuthenticationOptions = IdentityServer4.Configuration.AuthenticationOptions;

namespace accounts
{
    public class Startup
    {
        private readonly string _contentRootPath;

        private IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            _contentRootPath = env.ContentRootPath;

            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("clients.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
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
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddOptions();
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<ConnectionStringSettings>(Configuration.GetSection("Data:DefaultConnection"));
            services.Configure<OracleEnvironmentSettings>(Configuration.GetSection("OracleEnvironment"));
            services.Configure<List<Client>>(Configuration.GetSection("Clients"));

            //identityServer
            X509Certificate2 cert = new X509Certificate2(Path.Combine(_contentRootPath, "idsrv4test.pfx"), "idsrv3test");

            IIdentityServerBuilder builder = services
                .AddIdentityServer(options =>
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
                })
                .SetSigningCredential(cert);
            
            #region clients
            builder.Services.AddSingleton<IEnumerable<Client>>(provider => provider.GetService<Microsoft.Extensions.Options.IOptions<List<Client>>>().Value); 
            builder.Services.AddTransient<IClientStore, InMemoryClientStore>();
            builder.Services.AddTransient<ICorsPolicyService, InMemoryCorsPolicyService>();
            #endregion
            
            #region scopes
            builder.AddInMemoryScopes(Scopes.Get());
            #endregion

            #region users --> empty list
            builder.Services.AddSingleton(new List<InMemoryUser>());
            builder.Services.AddTransient<IProfileService, Services.ProfileService>();
            builder.Services.AddTransient<IResourceOwnerPasswordValidator, InMemoryResourceOwnerPasswordValidator>();
            #endregion

            // for the UI
            services
                .AddMvc(options => options.ModelBinderProviders.Insert(0, new EncryptModelBinderProvider()))
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
            services.Configure<RazorViewEngineOptions>(options => 
                options.FileProviders.Add(new EmbeddedFileProvider(GetType().Assembly, "accounts"))
            );
            
            services.AddRouting(
                options =>
                {
                    options.LowercaseUrls = true;
                    options.AppendTrailingSlash = true;
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
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
                app.UseExceptionHandler("/Home/Error");

            app.UseApplicationInsightsExceptionTelemetry();
            
            app.UseIdentityServer();

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
