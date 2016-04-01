using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DryIoc;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DryIoc.Dnx.DependencyInjection;
using Kit.Dal.DbManager;
using Kit.Kernel.CQRS.Command;
using Kit.Kernel.CQRS.Query;

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
            /*
            container.Register<IQueryDispatcher, QueryDispatcher>(reuse: Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
            container.Register<ICommandDispatcher, CommandDispatcher>(reuse: Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);*/
            
            container.Register(reuse: Reuse.InWebRequest, made: Made.Of(() => DbManagerFactory.CreateDbManager("Oracle.Data.Access")));
            
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
            services.AddMvc();
            
            // Add dependencies
            IContainer container = ConfigureDependencies(services);
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

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "Login",
                    template: "{controller=Auth}/{action=Login}");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
