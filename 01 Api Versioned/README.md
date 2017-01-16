# Construyendo un Web Api II: Versionado

    "Tus clientes más descontentos son tu mayor fuente de aprendizaje" 
    --Bill Gates

Proyectos anteriores: 

* [Construyendo un Web Api I: Starter](../00%20BoilerPlate)

![Api Version](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/webapi.jpg)

Para mí, el siguiente paso a dar al crear un API REST es versionarlo. El día de mañana no vamos a poder romper la estructura de nuestra API de golpe ya que nuestros clientes sufrirían un cambio catastrófico para ellos. Puede que existan algunos de los que incluso no se quieran actualizar y se mantengan en la versión anterior. Por ello esta vez vamos a versionar nuestra API.

El proceso de versionado es muy sencillo y veremos que no cuesta nada de trabajo actualizarlo. En este proyecto vamos a:

* __Sobreescribir el atributo Route__ para incorporarle versionado.
* Crear un __TestRunner__ para probar nuestros progresos. El TestRunner lo usaremos para probar las diferentes caracter�sticas, mientras que el proyecto principal ubicado en src lo dejaremos para poder cogerlo y reutilizarlo el día de mañana.
* Depurar con __Postman__ el resultado esperado.

## Versionado en API REST

Existen varias maneras de versionar un Api:

### Versionado en la URI 

Explicitar el versionado en la ruta. Por ejemplo: 

    .../maps/version/2
    .../maps/version/2/buildings/version/3
    .../maps/v2/buildings

Desde mi punto de vista este versionado es demasiado invasivo en la url del Api. Al fin y al cabo la url es para indicar dónde se encuentra un recurso y una versi�n no tiene nada que ver con el recurso, por lo que conceptualmente no sería del todo correcto poner la versión aquí.

### Parámetro en el Query String

Pasar el parámetro de la versión por el query string:

    .../maps/buildings?version=2

Habría que mirar cada vez el parámetro que indica la versión. De nuevo el problema principal es conceptual, los parámetros deberían ser para pedir un recurso, no para poder pedir una versión.

### Cabecera Accept

La cabecera Accept es la que usa el cliente para pedir el tipo de los datos en que quieren que le lleguen. Normalmente un ejemplo de esta cabecera es:

    Accept: application/json

En esta cabecera podríamos pedir también la versión en la que queremos que nos vengan los datos. En este caso en formato _json_. Esta cabecera también se puede usar para poner una especificación creada por nosotros:

    Accept: application/vnd.myapi.v2 + json

_vnd_ indica que es una definición propia y con ello podemos en nuestra Api leer el dato y darle la versión correspondiente.

Esta solución está cogida por pinzas, ya que cada cabecera debería tener un propósito y no mezclarlas.

### Cabecera personalizada

El protocolo HTTP permite poder crear cabeceras en las llamadas personalizadas, por lo que podemos crear nuestra propia cabecera:

    api-version: 2

Esta es la solución que a mí más me gusta y que implementaremos. Así, no mezclamos las cabeceras de Accept-Header ni invadimos los parámetros ni las urls. 

# ASP.NET Web Api: Atributo Route

Como vimos en el proyecto anterior, ASP.NET Web Api nos permite poder poner la ruta en cada uno de los métodos de nuestros Controladores:

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

El problema del atributo __Route__ es que no es suficiente. Si nuestra API evoluciona a una versión posterior después que nuestros clientes ya están en producción con una primera versión y los métodos cambian en nomenclatura, esto les afectaría en su funcionamiento. Por ello, debemos añadirle a cada una de estas funciones un nuevo parámetro que se corresponda con el número de la versión a la que pertenecen.

He creado en src una carpeta _Api.Features_ y he incluido en ella una carpeta _Versioned_ que contiene un proyecto llamado _Api.Versioned_ sobre el que vamos a trabajar.

También he añadido una carpeta Api.Helpers para ir incluyendo Helpers que podríamos utilizar en cualquier _Feature_ que vayamos implementando en el futuro.

![Api Versioned Project](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/ProjectVersionedApi.png)

La primera clase que vamos a crear es _VersionRoutedAttribute_, que va a implementar nuestra propia versión del Atributo Route:

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

El constructor del atributo recibirá dos parámetros: 

* Uno con la __dirección de la llamada__, que le pasaremos a nuestra clase base RoutedAttribute para que se encargue de ella. 
* Otro con la __versión__ de esa llamada. 

En el diccionario de restricciones vamos a añadir una restricción personalizada que indique cuál debe ser la versión correcta.

En el proyecto anterior vimos como las rutas podían especificarse poniendo valores por defecto y restricciones:

```csharp
config.Routes.MapHttpRoute(
   name: "DefaultApi",
   routeTemplate: "api/{controller}/{action}",
   defaults: new { id = RouteParameter.Optional },
*  constraint: new { id = "\d+" }
);
```

En el caso de DefaultApi la restricción es que el id sea un entero. Nosotros podemos crear nuestras propias restricciones cumpliendo el interfaz _IHttpRouteConstraint_. La clase VersionConstraint tendrá ese objetivo:

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

La clave está en el método Match: 

* Primero comprobamos si la dirección es la misma que tenemos establecida, si no es así devolvemos false ya que no es el método correcto. 
* Si coinciden en la dirección entonces tendremos que comprobar que versión viene en la llamada. Este sería el punto dónde tenemos que decidir lo visto en la parte del versionado de las APIs. Del parámetro _Request_ podríamos sacar cualquier tipo de información de la llamada. A mí me gusta poner una cabecera personalizada ya que deja la llamada más limpia. El método privado _GetVersion_ va a buscar una cabecera cuyo nombre sea la que hemos definido en las constantes _VersionConstants_.
* Si hay cabecera el valor se comparará con el valor del atributo que se les ha pasado. Si no hay cabecera se comparará con la versión por defecto de la API, es decir, si la API tiene un valor por defecto de 3 y queremos llamar a la versión 3 del API no hará falta que le pasemos ninguna cabecera con este 3. Esto normalmente se hace para no actualizar la versión 1 de las APIs y que se tengan que añadir cabeceras en versiones posteriores, para no tocar los clientes que ya usan la versión 1.

En la clase estática _VersionConstants_ tenemos los valores por defecto de la descripción de la cabecera (VersionHeader "api-header") y de la versión de la API por defecto (VersionDefault 1) si no viene ninguna cabecera:

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

El Helper _ConfigurationManagerHelper_ usa los otros dos valores de _VersionConstants_ para que se puedan sobreescribir desde el WebConfig. Si en el Web Config hay un registro clave-valor sobreescribiendo el api-description o el api-version-default se cogerá ese valor en lugar del de _VersionConstants_ 

```csharp
using Api.Helpers.Contracts.ConfigurationManagerHelpers;
using System;
using System.ComponentModel;
using System.Configuration;

namespace Api.Helpers.Core.ConfigurationManagerHelpers
{
    public class ConfigurationManagerHelper : IConfigurationManagerHelper
    {
        public T GetSettingOrDefaultValue<T>(string settingKey, T defaultValue)
        {
            var configValue = ConfigurationManager.AppSettings[settingKey];

            if (!string.IsNullOrWhiteSpace(configValue))
            {
                try
                {
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(configValue);
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException($"Configuration error: {settingKey}", e);
                }
            }

            return defaultValue;
        }
    }
}
```

Esta clase puede ser reutilizada en alguna otra _Feature_ que coja valores del WebConfig. Por eso la he separado de la _Feature_ de versionado. La posible reutilización siempre es importante, [habla con tus objetos](http://geeks.ms/windowsplatform/2016/05/11/habla-con-tus-objetos/).

Por último actualizamos nuestro controlador para dejarlo con el nuevo atributo:

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
*       [VersionedRoute(Constants.GetTestBoolean, VersionConstants.VersionDefault)]
        public async Task<IHttpActionResult> GetTestBoolean()
        {
            return await Task.FromResult(Ok(true));
        }

        [HttpGet]
        [ResponseType(typeof(bool))]
*       [VersionedRoute(Constants.GetTestBoolean, 2)]
        public async Task<IHttpActionResult> GetTestBoolean_V2()
        {
            return await Task.FromResult(Ok(false));
        }

        [HttpGet]
        [ResponseType(typeof(bool))]
*       [VersionedRoute(Constants.GetTestBooleanWithParam, VersionConstants.VersionDefault)]
        public async Task<IHttpActionResult> GetTestBooleanWithParam(int id)
        {
            return await Task.FromResult(Ok(true));
        }

        [HttpPost]
        [ResponseType(typeof(bool))]
*       [VersionedRoute(Constants.PostTestBoolean, VersionConstants.VersionDefault)]
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

En el webconfig de la aplicación he añadido una línea para poner la versión 2 por defecto:

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

Para probar el Api voy a usar _Postman_. Postman es una herramienta creada por Google que podéis descargar aquí: https://www.getpostman.com/

![Postman](http://blog.getpostman.com/wp-content/uploads/2014/07/logo.png)

Como ellos dicen en su propia Web: "Desarrolar APIs es difícil, Postman hace que sea más fácil". Para testear nuestros resultados es una herramienta muy potente ya que nos permite manejar las llamadas de una API y meter cabeceras en la misma de manera muy sencilla.

Vamos a configurar la primera llamada al Api:

![Llamada sin cabecera](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request1.png)

La primera llamada sin cabeceras recibe por defecto la versión 2, como está indicado en el webconfig. En cambio si añadimos la cabecera de que la versión que queremos es la 1:

![Llamada con cabecera versión 1](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request2.png)

Poner la cabecera con la versión 2 del Api o sin ella es equivalente:

![Llamada con cabecera versión 2](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request3.png)

Lo lógico es que la versión por defecto sea la 1 ya que en la primera versión puede no ser necesario añadir la cabecera. En cambio, al pasar a una versión posterior los clientes se tendrían que actualizar. Para no romper sus llamadas, lo más lógico sería añadir esa cabecera a partir de la segunda versión, evolucionando así nuestra API.

Si hacemos la llamadas con la cabecera con valor versión 3, el NotFoundController visto en el proyecto anterior entra en escena y nos devuelve un error 404, ya que para esa versión la url no existe:

![Llamada con cabecera versión 2](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request4.png)

# Conclusiones

Versionar el Api es muy sencillo y nos puede ahorrar muchos quebraderos de cabeza en el futuro. Por ello, mejor ponerlo desde el principio para no causar problemas a las aplicaciones clientes que consuman nuestra Api en el futuro.

Postman es una muy buena herramienta para depurar nuestras Apis y además tiene un interfaz muy fácil de usar y muy amigable.

# Referencias

* Versionado en APIs REST: http://blog.restcase.com/restful-api-versioning-insights/
* Atributo Route en ASP.NET Web Api 2: https://www.asp.net/web-api/overview/web-api-routing-and-actions/attribute-routing-in-web-api-2
* Postman: https://www.getpostman.com/
