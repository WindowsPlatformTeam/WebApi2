using System.Collections.Generic;
using System.Web.Http.Routing;

namespace Api.Versioned
{
    public class VersionedRoute : RouteFactoryAttribute
    {
        #region Fields
        private readonly int _allowedVersion;
        #endregion

        #region Constructor
        public VersionedRoute(string template, int allowedVersion)
            : base(template)
        {
            this._allowedVersion = allowedVersion;
        }
        #endregion

        #region Public Methods
        public override IDictionary<string, object> Constraints
        {
            get
            {
                var constraints = new HttpRouteValueDictionary { { "version", new VersionConstraint(_allowedVersion) } };
                return constraints;
            }
        }
        #endregion       
    }
}
