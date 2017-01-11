using Api.Controllers;
using Api.Core.Controllers.Base;
using Autofac;
using Autofac.Integration.WebApi;
using System.Net;
using System.Reflection;
using System.Web.Http;

namespace Api.App_Start
{
    public class DependencyContainer
    {
        public static IContainer BuildContainer(HttpConfiguration configuration)
        {
#if DEBUG
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif

            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(typeof(NotFoundController).GetTypeInfo().Assembly);
            builder.RegisterApiControllers(typeof(BaseController).GetTypeInfo().Assembly);
            builder.RegisterWebApiFilterProvider(configuration);

            // Register all services.

            var container = builder.Build();
            return container;
        }
    }
}