using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DryIoc;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DryIoc.Dnx.DependencyInjection;
using Kit.Dal.Configuration;
using Kit.Dal.DbManager;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Query;
using Kit.Kernel.Web;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Identity;

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
            container.Register<IQueryDispatcher, QueryDispatcher>(Reuse.InCurrentScope, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
            container.Register<ICommandDispatcher, CommandDispatcher>(Reuse.InCurrentScope, ifAlreadyRegistered: IfAlreadyRegistered.Replace);

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

            DataSet ds = (DataSet)System.Configuration.ConfigurationManager.GetSection("system.data");
            if (ds?.Tables.Count > 0)
            {
                DataTable dt = ds.Tables[0];

                bool invariantNameExists = dt.Columns.Contains("InvariantName");
                if (invariantNameExists)
                {
                    DataRow[] rows = dt.Select("InvariantName = 'Oracle.DataAccess.Client'");

                    foreach (DataRow row in rows)
                    {
                        dt.Rows.Remove(row);
                    }
                }

                dt.Rows.Add("Oracle Data Provider", "Oracle Data Provider for .NET", "Oracle.DataAccess.Client",
                    typeof (Oracle.DataAccess.Client.OracleClientFactory).AssemblyQualifiedName);
            }
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddAuthorization();
            
            services.AddMvc();
            
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<ConnectionStringSettings>(Configuration.GetSection("Data:DefaultConnection"));

            // Add dependencies
            IContainer container = ConfigureDependencies(services);

            // TODO Startup Tasks
            /*foreach (var dbManager in app.ApplicationServices.GetServices<ICommandDispatcher>())
            {
                              
            }*/

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
            {
                app.UseExceptionHandler("/Auth/Error");
            }
            
            app.UseIISPlatformHandler();

            app.UseApplicationInsightsExceptionTelemetry();

            app.UseStaticFiles();

            app.UseCookieAuthentication(o =>
            {
                o.AuthenticationScheme = "Cookies";
                o.AutomaticAuthenticate = true;
                o.AutomaticChallenge = true;
                
                //o.CookieName = "NTC." + Path.GetRandomFileName();

                o.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                o.Events = new CookieAuthenticationEvents()
                {
                    OnSignedIn = context =>
                    {
                        return Task.FromResult(0);
                    }
                };
            });

            

            app.UseMvc(routes =>
            {
                routes.MapRoute("Login", "{controller=Auth}/{action=Login}");
            });
            
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
