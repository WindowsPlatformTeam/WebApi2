using ApiTestRunner.Core.Controllers.Base;
using Autofac;
using Autofac.Integration.WebApi;
using System.Net;
using System.Reflection;
using System.Web.Http;

namespace ApiTestRunner
{
    public class DependencyContainer
    {
        public static IContainer BuildContainer(HttpConfiguration configuration)
        {
#if DEBUG
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif

            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(typeof(BaseController).GetTypeInfo().Assembly);
            builder.RegisterWebApiFilterProvider(configuration);

            // Register all services.

            var container = builder.Build();
            return container;
        }
    }
}