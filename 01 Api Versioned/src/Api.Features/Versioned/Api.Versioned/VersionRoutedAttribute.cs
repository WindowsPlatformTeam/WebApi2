using System.Collections.Generic;
using System.Web.Http.Routing;

namespace Api.Versioned
{
    public class VersionedRoute : RouteFactoryAttribute
    {
        #region Fields
        private readonly int allowedVersion;
        #endregion

        #region Constructor
        public VersionedRoute(string template, int allowedVersion)
            : base(template)
        {
            this.allowedVersion = allowedVersion;
        }
        #endregion

        #region Public Methods
        public override IDictionary<string, object> Constraints
        {
            get
            {
                var constraints = new HttpRouteValueDictionary { { "version", new VersionConstraint(allowedVersion) } };
                return constraints;
            }
        }
        #endregion       
    }
}
