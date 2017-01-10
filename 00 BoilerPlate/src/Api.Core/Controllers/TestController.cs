using Api.Core.Controllers.Base;
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
        [Route(Constants.GetTestBoolean)]
        public async Task<IHttpActionResult> GetTestBoolean()
        {
            return Ok(true);
        }

        [HttpGet]
        [ResponseType(typeof(bool))]
        [Route(Constants.GetTestBooleanWithParam)]
        public async Task<IHttpActionResult> GetTestBooleanWithParam(int id)
        {
            return Ok(true);
        }

        [HttpPost]
        [ResponseType(typeof(bool))]
        [Route(Constants.PostTestBoolean)]
        public async Task<IHttpActionResult> PostTestBoolean()
        {
            return Ok(true);
        }
    }
}

