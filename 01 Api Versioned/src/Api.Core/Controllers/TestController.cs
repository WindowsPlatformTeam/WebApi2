using Api.Core.Controllers.Base;
using Api.Versioned;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace Api.Core.Controllers
{
    [RoutePrefix(Constants.BaseRoute + "/" + Constants.Test)]
    public class TestController : BaseController
    {
        [HttpGet]
        [ResponseType(typeof(bool))]
        [VersionedRoute(Constants.GetTestBoolean, Constants.VersionDefault)]
        public async Task<IHttpActionResult> GetTestBoolean()
        {
            return await Task.FromResult(Ok(true));
        }

        [HttpGet]
        [ResponseType(typeof(bool))]
        [VersionedRoute(Constants.GetTestBooleanWithParam, Constants.VersionDefault)]
        public async Task<IHttpActionResult> GetTestBooleanWithParam(int id)
        {
            return await Task.FromResult(Ok(true));
        }

        [HttpPost]
        [ResponseType(typeof(bool))]
        [VersionedRoute(Constants.PostTestBoolean, Constants.VersionDefault)]
        public async Task<IHttpActionResult> PostTestBoolean()
        {
            return await Task.FromResult(Ok(true));
        }
    }
}

