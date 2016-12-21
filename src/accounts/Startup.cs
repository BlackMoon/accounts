﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using DryIoc;
using DryIoc.AspNetCore.DependencyInjection;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Services.InMemory;
using IdentityServer4.Stores;
using IdentityServer4.Stores.InMemory;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kit.Dal.DbManager;
using Kit.Core.CQRS.Command;
using Kit.Core.CQRS.Job;
using Kit.Core.CQRS.Query;
using Kit.Core.Identity;
using Kit.Core.Interception;
using Kit.Core.Interception.Attribute;
using Kit.Core.Web.Binders;
using Kit.Core.Web.Middleware.DebugMode;
using Kit.Core.Web.Middleware.ForceHttps;
using Kit.Core.Web.Mvc.Filters;
using Kit.Dal.Configuration;
using Kit.Dal.Oracle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

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
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("clients.json", true, true)
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
            IEnumerable<string> implAssembliesNames = new[] { "Kit.Core", "Kit.Dal" };

            // Register assemblies
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
            services.Configure<ForceHttpsOptions>(Configuration.GetSection("HttpsOptions"));
            services.Configure<OracleEnvironmentConfiguration>(Configuration.GetSection("OracleEnvironment"));
            services.Configure<List<Client>>(Configuration.GetSection("Clients"));
            
            //identityServer
            IIdentityServerBuilder builder = services
                .AddIdentityServer(options =>
                {
                    options.UserInteractionOptions.LoginUrl = "/login";
                    options.UserInteractionOptions.LogoutUrl = "/logout";
                    options.UserInteractionOptions.ConsentUrl = "/consent";
                    options.UserInteractionOptions.ErrorUrl = "/error";
                });

            #region certificate
            string fileName = Path.Combine(_contentRootPath, "idsrv4test.pfx");
            if (File.Exists(fileName))
            {
                X509Certificate2 cert = new X509Certificate2(fileName, "idsrv3test");
                builder.AddSigningCredential(cert);
            }
            else
                builder.AddTemporarySigningCredential();

            #endregion

            #region clients (from clients.json)
            builder.Services.AddSingleton<IEnumerable<Client>>(provider => provider.GetService<IOptions<List<Client>>>().Value); 
            builder.Services.AddTransient<IClientStore, InMemoryClientStore>();
            builder.Services.AddTransient<ICorsPolicyService, InMemoryCorsPolicyService>();
            #endregion

            #region resources
            builder.AddInMemoryIdentityResources(accounts.Configuration.Resources.GetIdentityResources());
            #endregion

            #region users --> empty list
            builder.AddProfileService<Services.ProfileService>();
            builder.Services.AddSingleton(new List<InMemoryUser>());
            builder.Services.AddTransient<IResourceOwnerPasswordValidator, InMemoryUserResourceOwnerPasswordValidator>();
            #endregion

            // for the UI
            services
                .AddMvc(options =>
                {
                    options.ModelBinderProviders.Insert(0, new EncryptModelBinderProvider());
                    options.CacheProfiles.Add("1hour", new CacheProfile() {Duration = 3600});
                })
                .AddJsonOptions(option =>
                {
                    option.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    option.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });
            
            services.AddRouting(options => options.LowercaseUrls = true);
            services.Configure<MvcOptions>(options => options.Filters.Add(new GlobalExceptionFilter()));            // Global exceptions' filter
            services.Configure<RazorViewEngineOptions>(options => options.FileProviders.Add(new EmbeddedFileProvider(GetType().Assembly, "accounts")));

            // Add dependencies
            IContainer container = ConfigureDependencies(services);

            // Startup Jobs
            IJobDispatcher dispatcher = container.Resolve<IJobDispatcher>(IfUnresolved.ReturnDefault);
            dispatcher?.Dispatch<IStartupJob>();

            return container.Resolve<IServiceProvider>();            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
           
            app.UseApplicationInsightsRequestTelemetry();
            
            // check development (debug) mode
            app.CheckDebugMode();

            // forceHttps
            if (Configuration["HttpsOptions:Port"] != null)
            {
                IOptions<ForceHttpsOptions> options = app.ApplicationServices.GetService<IOptions<ForceHttpsOptions>>();
                app.UseForceHttps(options.Value);
            }
            
            // exception handlers
            app.UseStatusCodePagesWithReExecute("/error/{0}");
        
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
                app.UseExceptionHandler("/error");

            app.UseApplicationInsightsExceptionTelemetry();
            
            app.UseIdentityServer();
            app.UseStaticFiles();
            
            app.UseMvc(routes =>
            {
                routes.MapRoute("login", "login", new { controller = "Login", action = "Index" });
                routes.MapRoute("logout", "logout", new { controller = "Logout", action = "Index" });
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public static void Main(string[] args)
        {
            Console.Title = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            IConfigurationRoot hostConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("hosting.json", optional: true)
                .Build();

            IWebHost host = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(hostConfig)
                .UseUrls(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
