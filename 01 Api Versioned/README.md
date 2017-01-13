# Construyendo un Web Api II: Versionado

    "Tus clientes m�s descontentos son tu mayor fuente de aprendizaje" 
    --Bill Gates

Proyectos anteriores: 

* [Construyendo un Web Api I: Starter](../00%20BoilerPlate)

![Api Version](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/webapi.jpg)

Para m�, el siguiente paso a dar al crear un API REST es versionarlo. El d�a de ma�ana no vamos a poder romper la estructura de nuestra API de golpe ya que nuestros clientes sufrir�an un cambio catastr�fico para ellos. Puede que existan algunos de los que incluso no se quieran actualizar y se mantengan en la versi�n anterior. Por ello esta vez vamos a versionar nuestra API.

El proceso de versionado es muy sencillo y veremos que no cuesta nada de trabajo actualizarlo. En este proyecto vamos a:

* __Sobreescribir el atributo Route__ para incorporarle versionado.
* Crear un __TestRunner__ para probar nuestros progresos. El TestRunner lo usaremos para probar las diferentes caracter�sticas, mientras que el proyecto principal ubicado en src lo dejaremos para poder cogerlo y reutilizarlo el d�a de ma�ana.
* Depurar con __Postman__ el resultado esperado.

## Versionado en API REST

Existen varias maneras de versionar un Api:

### Versionado en la URI 

Explicitar el versionado en la ruta. Por ejemplo: 

    .../maps/version/2
    .../maps/version/2/buildings/version/3
    .../maps/v2/buildings

Desde mi punto de vista este versionado es demasiado invasivo en la url del Api. Al fin y al cabo la url es para indicar d�nde se encuentra un recurso y una versi�n no tiene nada que ver con el recurso, por lo que conceptualmente no ser�a del todo correcto poner la versi�n aqu�.

### Par�metro en el Query String

Pasar el par�metro de la versi�n por el query string:

    .../maps/buildings?version=2

Habr�a que mirar cada vez el par�metro que indica la versi�n. De nuevo el problema principal es conceptual, los par�metros deber�an ser para pedir un recurso, no para poder pedir una versi�n.

### Cabecera Accept

La cabecera Accept es la que usa el cliente para pedir el tipo de los datos en que quieren que le lleguen. Normalmente un ejemplo de esta cabecera es:

    Accept: application/json

En esta cabecera podr�amos pedir tambi�n la versi�n en la que queremos que nos vengan los datos. En este caso en formato _json_. Esta cabecera tambi�n se puede usar para poner una especificaci�n creada por nosotros:

    Accept: application/vnd.myapi.v2 + json

_vnd_ indica que es una definici�n propia y con ello podemos en nuestra Api leer el dato y darle la versi�n correspondiente.

Esta soluci�n est� cogida por pinzas, ya que cada cabecera deber�a tener un prop�sito y no mezclarlas.

### Cabecera personalizada

El protocolo HTTP permite poder crear cabeceras en las llamadas personalizadas, por lo que podemos crear nuestra propia cabecera:

    api-version: 2

Esta es la soluci�n que a m� m�s me gusta y que implementaremos. As�, no mezclamos las cabeceras de Accept-Header ni invadimos los par�metros ni las urls. 

# ASP.NET Web Api: Atributo Route

Como vimos en el proyecto anterior, ASP.NET Web Api nos permite poder poner la ruta en cada uno de los m�todos de nuestros Controladores:

```csharp
public static class WebApiConfig
{
   public static void Register(HttpConfiguration config)
   {
      ...
 
      config.MapHttpAttributeRoutes();
 
      ...
 
    }
}
 
[RoutePrefix(Constants.BaseRoute + "/" + Constants.Test)]
public class TestController : BaseController
{
   [HttpGet]
   [ResponseType(typeof(bool))]
*  [Route(Constants.GetTestBoolean)]
   public async Task<IHttpActionResult> GetTestBoolean()
   {
      return Ok(true);
   }
}
```

El problema de este atributo Route es que no es suficiente, ya que si nuestra API evoluciona a una versi�n posterior despu�s que nuestros clientes ya est�n en producci�n con una primera versi�n y los m�todos cambian en nomenclatura, esto les afectar�a en su funcionamiento. Por ello debemos a�adirle a cada una de estas funciones un nuevo par�metro que se corresponda con el n�mero de la versi�n a la que pertenecen.

Para ello he creado en src una carpeta Api.Features e incluido en ella Versioned un proyecto llamado Api.Versioned sobre el que vamos a trabajar.

![Api Versioned Project](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/ProjectVersionedApi.png)

La primera clase que tenemos que crear es VersionRoutedAttribute, que va a implementar nuestra propia versi�n del Atributo Route:

```csharp
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
```

El constructor del atributo recibir� dos par�metro, uno con la direcci�n de la llamada -que le pasaremos a nuestra clase base RoutedAttribute para que se encargue de ella- y otro con la versi�n de esa llamada. En el diccionario de restricciones le vamos a meter una restricci�n personalizada que indique cu�l debe ser la versi�n correcta.

En el proyecto anterior vimos como las rutas pod�an especificarse poniendo valores por defecto y restricciones:

```csharp
config.Routes.MapHttpRoute(
   name: "DefaultApi",
   routeTemplate: "api/{controller}/{action}",
   defaults: new { id = RouteParameter.Optional },
*  constraint: new { id = "\d+" }
);
```

En el caso de DefaultApi la restricci�n es que el id sea un entero. Nosotros podemos crear nuestras propias restricciones cumpliendo el interfaz _IHttpRouteConstraint_, para ello implementaremos la clase VersionConstraint:

```csharp
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
```

La clave est� en el m�todo Match: 

* Primero comprobamos si la direcci�n es la misma que tenemos establecida, si no es as� devolvemos false ya que no es el m�todo correcto. 
* Si coinciden en la direcci�n entonces tendremos que comprobar que versi�n viene en la llamada. �C�mo viene la versi�n en la llamada? A m� me gusta poner una cabecera. As�, el m�todo privado GetVersion va a buscar una cabecera cuyo nombre sea la que hemos definido en las constantes VersionConstants.
* Si hay cabecera el valor se comparar� con el valor del atributo que se les ha pasado. Si no hay cabecera se comparar� con la versi�n por defecto de la API, es decir, si la API tiene un valor por defecto de 3 y queremos llamar a la versi�n 3 del api no har� falta que le pasemos ninguna cabecera con este 3. Esto normalmente se hace para no actualizar la versi�n 1 de las APIs y que se tengan que a�adir cabeceras en versiones posteriores.

En la clase est�tica VersionConstants tenemos los valores por defecto de la descripci�n de la cabecera (VersionHeader "api-header") y de la versi�n del api por defecto (VersionDefault 1) si no viene ninguna cabecera:

```csharp
namespace Api.Versioned
{
    public static class VersionConstants
    {
        //
        // Configuration Settings
        //
        public const string ConfVersionHeader = "configuration:api-version-header-description";
        public const string ConfVersionDefault = "configuration:api-version-default";

        //
        // Versioned Constants
        //
        public const string VersionHeader = "api-version";
        public const int VersionDefault = 1;
    }
}
```

El Helper _ConfigurationManagerHelper_ usa los otros dos valores de __VersionConstants__ para que se puedan sobreescribir desde el WebConfig. Si en el Web Config hay un registro clave-valor sobreescribiendo el api-description o el api-version-default se coger� ese valor en lugar del de _VersionConstants_ 

Por �ltimo actualizamos el valor de nuestro controlador para dejarlo con el nuevo atributo:

```csharp
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
        [VersionedRoute(Constants.GetTestBoolean, VersionConstants.VersionDefault)]
        public async Task<IHttpActionResult> GetTestBoolean()
        {
            return await Task.FromResult(Ok(true));
        }

        [HttpGet]
        [ResponseType(typeof(bool))]
        [VersionedRoute(Constants.GetTestBoolean, 2)]
        public async Task<IHttpActionResult> GetTestBoolean_V2()
        {
            return await Task.FromResult(Ok(false));
        }

        [HttpGet]
        [ResponseType(typeof(bool))]
        [VersionedRoute(Constants.GetTestBooleanWithParam, VersionConstants.VersionDefault)]
        public async Task<IHttpActionResult> GetTestBooleanWithParam(int id)
        {
            return await Task.FromResult(Ok(true));
        }

        [HttpPost]
        [ResponseType(typeof(bool))]
        [VersionedRoute(Constants.PostTestBoolean, VersionConstants.VersionDefault)]
        public async Task<IHttpActionResult> PostTestBoolean()
        {
            return await Task.FromResult(Ok(true));
        }
    }
}
```

# TestRunner Versionado

Para poder testear la nueva funcionalidad vamos a crear una carpeta test/TestRunner con una copia de nuestra Api. Dentro vamos a poner un controlador con el nuevo atributo Versioned para poder probarlo:

```csharp
namespace ApiTestRunner.Core
{
    public static class Constants
    {
        public const string ApiName = "API";

        public const string BaseRoute = "api";
        public const string VersionHeader = "api-header";
        public const int VersionDefault = 1;

        public const string Version = "version";
        public const string GetTest = "test";
    }
}
------------------------------------------------------------
using ApiTestRunner.Core.Controllers.Base;
using Api.Versioned;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace ApiTestRunner.Core.Controllers
{
    [RoutePrefix(Constants.BaseRoute + "/" + Constants.Version)]
    public class VersionController : BaseController
    {
        [HttpGet]
        [ResponseType(typeof(bool))]
        [VersionedRoute(Constants.GetTest, 1)]
        public async Task<IHttpActionResult> GetTestBoolean()
        {
            return await Task.FromResult(Ok("This is version 1"));
        }

        [HttpGet]
        [ResponseType(typeof(bool))]
        [VersionedRoute(Constants.GetTest, 2)]
        public async Task<IHttpActionResult> GetTestBoolean2()
        {
            return await Task.FromResult(Ok("This is version 2"));
        }
    }
}
```

En el webconfig de la aplicaci�n he a�adido una l�nea para poner la versi�n 2 por defecto:

```xml
<?xml version="1.0" encoding="utf-8"?>

<configuration>

  ...

  <appSettings>
    <add key="configuration:api-version-default" value="2"/>
  </appSettings>

  ...

</configuration>
```

Para probar el Api voy a usar _Postman_. Postman es una herramienta creada por Google que pod�is descargar aqu�: https://www.getpostman.com/

![Postman](http://blog.getpostman.com/wp-content/uploads/2014/07/logo.png)

Como ellos dicen en su propia Web: "Desarrolar APIs es dif�cil, Postman hace que sea m�s f�cil". Para testear nuestros resultados es una herramienta muy potente ya que nos permite manejar las llamadas de un Api y meter cabeceras en la misma de manera muy sencilla.

Vamos a configurar la primera llamada al Api:

![Llamada sin cabecera](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request1.png)

La primera llamada sin cabeceras coge por defecto la versi�n 2, como est� indicado en el webconfig. En cambio si a�adimos la cabecera de que la versi�n que queremos es la 1:

![Llamada con cabecera versi�n 1](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request2.png)

Poner la cabecera con la versi�n 2 del Api o sin ella es equivalente:

![Llamada con cabecera versi�n 2](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request3.png)

Lo l�gico es que la versi�n por defecto sea la 1 ya que en la primera versi�n puede no ser necesario a�adir la cabecera. En cambio, al pasar a una versi�n posterior los clientes se tendr�an que actualizar y para no romper sus llamadas, lo m�s l�gico ser�a a�adir esa cabecera a partir de la segunda versi�n, evolucionando as� nuestra Api.

# Conclusiones

Versionar el Api es muy sencillo y nos puede ahorrar muchos quebraderos de cabeza en el futuro. Por ello, mejor ponerlo desde el principio para no causar problemas a las aplicaciones clientes que consuman nuestra Api en el futuro.

Postman es una muy buena herramienta para depurar nuestras Apis y adem�s tiene un interfaz muy f�cil de usar y muy amigable.

# Referencias

* Versionado en APIs REST: http://blog.restcase.com/restful-api-versioning-insights/
* Atributo Route en ASP.NET Web Api 2: https://www.asp.net/web-api/overview/web-api-routing-and-actions/attribute-routing-in-web-api-2
* Postman: https://www.getpostman.com/
