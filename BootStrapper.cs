using Nancy;
using Nancy.Bootstrappers.DryIoc;

namespace Accounts
{
    public class VNextRootPathProvider : IRootPathProvider
    {
        public string GetRootPath()
        {
            return Startup.AppEnvironment.ApplicationBasePath;
        }
    }

    /// <summary>
    /// BootStrapper class
    /// </summary>
    public class BootStrapper : DryIocNancyBootstrapper
    {
        protected override IRootPathProvider RootPathProvider => new VNextRootPathProvider();
    }
}
