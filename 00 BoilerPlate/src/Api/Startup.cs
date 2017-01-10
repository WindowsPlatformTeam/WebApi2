using Api;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using System.Web.Http;

[assembly: OwinStartup(typeof(Startup))]
namespace Api
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            WebApiConfig.Register(config);
            app.UseCors(CorsOptions.AllowAll);
            app.UseWebApi(config);
#if DEBUG
            SwaggerConfig.Configure(config);
#endif
        }
    }
}
