# Construyendo un Web Api II: Versionado

    "Tus clientes más descontentos son tu mayor fuente de aprendizaje" 
    --Bill Gates

Proyectos anteriores: 

* [Construyendo un Web Api I: Starter](../00%20BoilerPlate)

![Api Version](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/webapi.jpg)

Para mí, el siguiente paso a dar al crear un API REST es versionarlo. El día de mañana no vamos a poder romper la estructura de nuestra API de golpe ya que nuestros clientes sufrirían un cambio catastrófico para ellos. Puede que existan algunos de los que incluso no se quieran actualizar y se mantengan en la versión anterior. Por ello esta vez vamos a versionar nuestra API.

El proceso de versionado es muy sencillo y veremos que no cuesta nada de trabajo actualizarlo. En este proyecto vamos a:

* __Sobreescribir el atributo Route__ para incorporarle versionado.
* Depurar con __Postman__ el resultado esperado.
* Crear un __TestRunner__ para probar nuestros progresos. El TestRunner lo usaremos para probar las diferentes características, mientras que el proyecto principal ubicado en src lo dejaremos para poder cogerlo y reutilizarlo el día de mañana.

## ASP.NET Web Api: Atributo Route

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

El problema de este atributo Route es que no es suficiente, ya que si nuestra API evoluciona a una versión posterior después que nuestros clientes ya estén en producción con una primera versión y los métodos cambian en nomenclatura, esto les afectaría en su funcionamiento. Por ello debemos añadirle a cada una de estas funciones un nuevo parámetro que se corresponda con el número de la versión a la que pertenecen.

Para ello he creado en src una carpeta Api.Features e incluido en ella Versioned un proyecto llamado Api.Versioned sobre el que vamos a trabajar.

![Api Versioned Project](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/ProjectVersionedApi.png)

La primera clase que tenemos que crear es VersionRoutedAttribute, que va a implementar nuestra propia versión del Atributo Route:

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

El constructor del atributo recibirá dos parámetro, uno con la dirección de la llamada -que le pasaremos a nuestra clase base RoutedAttribute para que se encargue de ella- y otro con la versión de esa llamada. En el diccionario de restricciones le vamos a meter una restricción personalizada que indique cuál debe ser la versión correcta.

En el proyecto anterior vimos como las rutas podían especificarse poniendo valores por defecto y restricciones:

```csharp
config.Routes.MapHttpRoute(
   name: "DefaultApi",
   routeTemplate: "api/{controller}/{action}",
   defaults: new { id = RouteParameter.Optional },
*  constraint: new { id = "\d+" }
);
```

En el caso de DefaultApi la restricción es que el id sea un entero. Nosotros podemos crear nuestras propias restricciones cumpliendo el interfaz _IHttpRouteConstraint_, para ello implementaremos la clase VersionConstraint:

```csharp
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
```

La clave está en el método Match: 

* Primero comprobamos si la dirección es la misma que tenemos establecida, si no es así devolvemos false ya que no es el método correcto. 
* Si coinciden en la dirección entonces tendremos que comprobar que versión viene en la llamada. ¿Cómo viene la versión en la llamada? A mí me gusta poner una cabecera. Así, el método privado GetVersion va a buscar una cabecera cuyo nombre sea la que hemos definido en las constantes VersionConstants.
* Si hay cabecera el valor se comparará con el valor del atributo que se les ha pasado. Si no hay cabecera se comparará con la versión por defecto de la API, es decir, si la API tiene un valor por defecto de 3 y queremos llamar a la versión 3 del api no hará falta que le pasemos ninguna cabecera con este 3. Esto normalmente se hace para no actualizar la versión 1 de las APIs y que se tengan que añadir cabeceras en versiones posteriores.

En la clase estática VersionConstants he añadido un poco de azúcar sintáctico:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

namespace Api.Versioned
{
    public static class VersionConstants
    {
        static Dictionary<string, object> _defaultValues = new Dictionary<string, object>()
        {
            { ConfVersionHeader, VersionConstants.VersionHeader },
            { ConfVersionDefault, VersionConstants.VersionDefault }
        };

        //
        // Configuration Settings
        //
        public const string ConfVersionHeader = "configuration:api-header";
        public const string ConfVersionDefault = "configuration:api-version-default";

        //
        // Versioned Constants
        //
        public const string VersionHeader = "api-header";
        public const int VersionDefault = 1;

        public static T GetSettingOrDefaultValue<T>(string settingKey)
        {
            var configValue = ConfigurationManager.AppSettings[settingKey];

            if (!string.IsNullOrWhiteSpace(configValue))
            {
                try
                {
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(configValue);
                }
                catch (Exception)
                {

                }
            }

            object value;

            _defaultValues.TryGetValue(settingKey, out value);

            if (value != null)
                return (T)Convert.ChangeType(value, typeof(T));
            throw new ConfigurationErrorsException($"Configuration not found: {settingKey}");
        }
    }
}
```

El método GetSettingOrDefaultValue sirve para que cojamos los valores de VersionHeader ("api-header") y VersionDefault (1) por defecto; pero si queremos sobreescribirlos podemos poner un valor en el Web.config para sobreescribir estos parámetros. Veremos esta opción más adelante en el TestRunner.

Por último actualizamos el valor de nuestro controlador para dejarlo con el nuevo atributo:

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

## TestRunner Versionado

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

Como ellos dicen en su propia Web: "Desarrolar APIs es difícil, Postman hace que sea más fácil". Para testear nuestros resultados es una herramienta muy potente ya que nos permite manejar las llamadas de un Api y meter cabeceras en la misma de manera muy sencilla.

Vamos a configurar la primera llamada al Api:

![Llamada sin cabecera](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request1.png)

La primera llamada sin cabeceras coge por defecto la versión 2, como está indicado en el webconfig. En cambio si añadimos la cabecera de que la versión que queremos es la 1:

![Llamada con cabecera versión 1](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request2.png)

Poner la cabecera con la versión 2 del Api o sin ella es equivalente:

![Llamada con cabecera versión 2](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2017/01/Postman_request3.png)

Lo lógico es que la versión por defecto sea la 1 ya que en la primera versión puede no ser necesario añadir la cabecera. En cambio, al pasar a una versión posterior los clientes se tendrían que actualizar y para no romper sus llamadas, lo más lógico sería añadir esa cabecera a partir de la segunda versión, evolucionando así nuestra Api.

## Conclusiones

Versionar el Api es muy sencillo y nos puede ahorrar muchos quebraderos de cabeza en el futuro. Por ello, mejor ponerlo desde el principio para no causar problemas a las aplicaciones clientes que consuman nuestra Api en el futuro.

Postman es una muy buena herramienta para depurar nuestras Apis y además tiene un interfaz muy fácil de usar y muy amigable.

## Referencias

* Atributo Route en ASP.NET Web Api 2: https://www.asp.net/web-api/overview/web-api-routing-and-actions/attribute-routing-in-web-api-2
* Postman: https://www.getpostman.com/
