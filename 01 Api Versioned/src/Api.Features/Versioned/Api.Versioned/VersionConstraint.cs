using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Routing;

namespace Api.Versioned
{
    internal class VersionConstraint : IHttpRouteConstraint
    {
        #region Fields
        private readonly int allowedVersion;
        #endregion

        #region Constructor
        public VersionConstraint(int allowedVersion)
        {
            this.allowedVersion = allowedVersion;
        }
        #endregion

        #region Public Methods
        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName,
            IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            if (routeDirection != HttpRouteDirection.UriResolution) return false;
            var version = GetVersionHeader(request) ?? VersionConstants.GetSettingOrDefaultValue<int>(VersionConstants.ConfVersionDefault);
            return version == allowedVersion;
        }
        #endregion

        #region Private Methods
        private int? GetVersionHeader(HttpRequestMessage request)
        {
            string versionAsString;
            IEnumerable<string> headerValues;
            if (request.Headers.TryGetValues(VersionConstants.GetSettingOrDefaultValue<string>(VersionConstants.ConfVersionHeader), out headerValues) && 
                headerValues.Count() == 1)
            {
                versionAsString = headerValues.First();
            }
            else
            {
                return null;
            }

            int version;
            if (versionAsString != null && int.TryParse(versionAsString, out version))
            {
                return version;
            }

            return null;
        }
        #endregion
    }
}
