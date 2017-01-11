using System.Web.Http;
using Swashbuckle.Application;

namespace ApiTestRunner
{
    public class SwaggerConfig
    {
        public static void Configure(HttpConfiguration config)
        {
            config
            .EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", Core.Constants.ApiName);
                c.Schemes(new[] { "http", "https" });
            })
            .EnableSwaggerUi();
        }
    }
}
