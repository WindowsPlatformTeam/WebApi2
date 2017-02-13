using Api.Helpers.Contracts.ConfigurationManagerHelpers;
using Api.Helpers.Core.ConfigurationManagerHelpers;
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

            IConfigurationManagerHelper configurationManagerHelper = request.GetDependencyScope().GetService(typeof(IConfigurationManagerHelper)) as IConfigurationManagerHelper;
            if (configurationManagerHelper == null) configurationManagerHelper = new ConfigurationManagerHelper();

            var version = GetVersionHeader(request, configurationManagerHelper) ?? configurationManagerHelper.GetSettingOrDefaultValue(VersionConstants.ConfVersionDefault, VersionConstants.VersionDefault);
            return version == allowedVersion;
        }
        #endregion

        #region Private Methods
        private int? GetVersionHeader(HttpRequestMessage request, IConfigurationManagerHelper configurationManagerHelper)
        {
            string versionAsString;
            IEnumerable<string> headerValues;
            var headerApiVersion = configurationManagerHelper.GetSettingOrDefaultValue(VersionConstants.ConfVersionHeader, VersionConstants.VersionHeader);
            if (request.Headers.TryGetValues(headerApiVersion, out headerValues) && 
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
