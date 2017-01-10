# Construyendo un Web Api I: Starter

    "A programar se aprende programando"

Cuantas veces hemos escuchado esa frase. Yo también comparto esa afirmación por lo que me he propuesto desarrollar un web api con ASP.NET Web Api desde cero con todos los conocimientos que he ido adquiriendo estos años, añadiéndole modularmente funcionalidades. Cometeré errores por el proceso por lo que todo feedback será bienvenido. Empezamos.

Desde la entrada de las SPA en el mundo web las Web APIs han sido una de las maneras más utilizadas como arquitectura para la parte servidor. Al final trabajar con servicios HTTP se hace muy cómodo y tanto aplicaciones web, aplicaciones móviles, una aplicación de escritorio pueden consumir datos desde este tipo de soluciones.

Para este primer artículo me gustaría marcarme el objetivo de poder crear un Starter para Web Api reutilizable.

## ASP.NET WEB API

¿Qué es ASP.NET Web Api? Según la propia definición de Microsoft: 

> ASP.NET Web API es un marco que facilita la creación de servicios HTTP disponibles para una amplia variedad de clientes, entre los que se incluyen exploradores y dispositivos móviles. ASP.NET Web API es la plataforma perfecta para crear aplicaciones RESTful en .NET Framework.

En otras palabras es la abstracción de Microsoft para poder trabajar con el protocolo HTTP y esto lo tendríamos que tener siempre muy presente. Estamos adaptando el mundo C# al protocolo HTTP por lo que siempre habrá que tener el protocolo muy presente.

ASP.NET Web API está dentro del marco ASP.NET MVC basado en el patrón MVC (Modelo-Vista-Controlador). En el caso de ASP.NET Web API no tenemos la V de vista, ya que nosotros ofreceremos una serie de endpoints que se pueden consumir desde cualquier cliente. Los Modelos y los Controladores funcionan igual que en el marco de ASP.NET MVC aunque yo los voy a adaptar a mi manera de trabajar. En casi todos los tutoriales de Microsoft suelen dejar los modelos en el mismo proyecto web y esto es algo que voy a cambiar. Nuestros Modelos tendrán su proyecto aparte para ponerlos en nuestro dominio. Los controladores también los vamos a separar para crear un proyecto “Startup” desacoplado de los mismos. Muchos detalles que iremos viendo poco a poco.

Una cosa que suele pasar con las soluciones de ASP.NET Web Api es que van muy ligadas a sus correspondiente SPA. Y esto es un error. Un Web Api es un proyecto por sí mismo y hay que darle la importancia que se merece abstrayéndolo de los clientes que lo van a consumir. Nunca sabemos que nos va a deparar el futuro y hacer las cosas bien desde el principio puede hacer que nos podamos adaptar a tener varios clientes consumiendo nuestra API sin ningún problema.

A la hora de crear un Web Api con ASP.NET Web Api a mí me gusta crear dos proyectos: Api y Api.Core:

* El proyecto de Api será de tipo ASP.NET MVC. Este proyecto contendrá la clase Startup.cs donde se pone en marcha todo el mecanismo de MVC con Owin. La configuración del Web Api con la inyección de dependecias, la configuración de las rutas y otras herramientas de terceros como Swagger también estarán en este proyecto.

* El proyecto de Api.Core será un proyecto de tipo “Class Library” que contendrá todos los controladores que definirán los endpoints.

Esta separación modular hace que los controladores estén aislados de la versión de ASP.NET Web Api que usemos. Ya podemos usar MVC 4 ó 5 que los controladores están completamente desacoplados de ello. Si hubiera una versión de MVC 6 compatible con nuestros controladores sólo tendríamos que tocar el proyecto de Api. Por tanto nuestra estructura de proyectos queda como la imagen de nuestra derecha.

![estructura del proyecto](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2016/12/estructura_proyecto.png "Estructura Proyecto")

Esta estructura de proyectos se la vi por primera vez a mi compañero Francisco Olmedo ([@fmolmedo](https://twitter.com/fmolmedo)) y la verdad es que me quedé sorprendido. Hay muchos conceptos detrás de todo esto. Sobre todo tener muy claro la modularidad y reutilización a un nivel muy alto. Y como hay que copiar a los mejores la he adoptado para nuestra solución.

En resumen estos dos proyectos separan dos conceptos: startup y endpoints. Por un lado queda el código que hace arrancar y configura nuestra API. En el otro lado quedan expuestos los endpoints. Simple separación de conceptos.

## STARTER WEB API CON OWIN

Vamos a ir creando el proyecto de Api. Para ello creamos un proyecto de MVC eligiendo la opción Empty y marcando el check de Web Api:

![creación del proyecto](http://geeks.ms/windowsplatform/wp-content/uploads/sites/266/2016/12/initializeWebApiProject.png "Creación del Proyecto")

1 Eliminamos las carpetas de Models y App_data. Eliminamos también el Global.asax ya que vamos a utilizar la especificación Owin para indicarle a Web Api cual va a ser su clase Startup. Para ello abrimos Nuget y añadimos los siguientes paquetes:
  * Owin
  * Microsoft.AspNet.WebApi.Owin
  * Microsoft.Owin.Cors
  * Microsoft.Owin.Host.SystemWeb

2 Con esto tendríamos todos los paquetes de Owin necesarios. ¿Y qué es esto de Owin? “OWIN (Open Web Interface for .NET) es una especificación abierta iniciada por dos miembros del equipo de ASP.NET en Microsoft, Louis Dejardin y Benjamin Vanderveen, que define un interfaz estándar para comunicar servidores con aplicaciones .NET, inspirada en iniciativas similares procedentes de tecnologías como Ruby, Python o incluso Node.” Vamos a utilizarlo mucho, por lo que os recomiendo la lectura completa del artículo de la definición de José María Aguilar: http://www.variablenotfound.com/2013/09/owin-i-introduccion.html

3 Como inyector de dependencias vamos a utilizar Autofac. ¿Inyección de dependencias? http://anexsoft.com/p/97/ejemplo-de-inyeccion-de-dependencias-con-c Quédate con la idea y la iremos usando poco a poco. Este es el paquete que instalaremos:
  * Autofac.WebApi2 (Cuidado con no instalar Autofac.WebApi)

4 Por último, voy a añadir Swagger. Swagger es una herramienta muy útil que nos puede servir para documentar y testear nuestra API. Podéis echarle un vistazo aquí http://swagger.io/  Swagger nos va a proveer un interfaz de usuario con los endpoints que tenemos expuestos al usuario. Lo veremos en detalle también más adelante. El paquete a añadir es:
  * Swahbuckle

Con esto tenemos todos los paquetes necesarios para empezar a trabajar con el proyecto Api. Teniendo un poco de cuidado con que Nuget no nos juegue malas pasadas empezamos con la clase Startup:

```csharp
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
```

Si nos fijamos en la anotación de la clase **[assembly: OwinStartup(typeof(Startup))]** esto es lo que hace que no tengamos que usar un Global.asax para indicar la entrada de la aplicación sino que con esta anotación Owin ya sabe que Startup.cs es su punto de partida. La magia la hace la librería que hemos instalado anteriormente de Owin *Microsoft.Owin.Host.SystemWeb*.

La línea *app.UseCors(CorsOptions.AllowAll);* nos permite poder usar el mecanismo de **Intercambio de Recursos de Orígenes Cruzados (CORS, por sus siglas en inglés)**. Por razones de seguridad, el acceso a los datos del API solo puede hacerse desde su propio dominio; pero por la insistente demanda de los desarrolladores web CORS habilita el acceso a dominios cruzados entre servidores web, lo que hace posible la transferencia segura de datos entre dominios cruzados. Podemos ver CORS en más detalle aquí: https://developer.mozilla.org/es/docs/Web/HTTP/Access_control_CORS

La última línea establece la configuración de Swagger definida en la clase SwaggerConfig. **Swagger** suele dejarse sólo para los entornos de debug por eso tiene el IF DEBUG entre medias.

A continuación os muestro la clase SwaggerConfig:

```csharp
using System.Web.Http;
using Swashbuckle.Application;
 
namespace Api
{
   public class SwaggerConfig
   {
      public static void Configure(HttpConfiguration config)
      {
          config
          .EnableSwagger(c =&amp;amp;amp;amp;amp;gt;
          {
             c.SingleApiVersion("v1", Core.Constants.ApiName);
             c.Schemes(new[] { "http", "https" });
          })
          .EnableSwaggerUi();
      }
   }
}
```

En el archivo de Swagger dejaremos la configuración por defecto quitando los comentarios y añadiendo el nombre de la API que está en la clase Constants en el Api.Core. Es una manía mía pero me gusta ponerle nombre a las APIs. La configuración de nuestro web api queda muy sencilla en la clase WebApiConfig:

```csharp
using Api.App_Start;
using Autofac.Integration.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
 
namespace Api
{
   public static class WebApiConfig
   {
      public static void Register(HttpConfiguration config)
      {
         config.DependencyResolver = new AutofacWebApiDependencyResolver(DependencyContainer.BuildContainer(config));
 
         config.MapHttpAttributeRoutes();
 
         config.Routes.MapHttpRoute(
            name: "Error404",
            routeTemplate: "api/{*url}",
            defaults: new { controller = "NotFound", action = "ErrorNotFound" }
         );
 
         var jsonFormatter = config.Formatters.OfType<JsonMediaTypeFormatter>().First();
         jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
         jsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
         jsonFormatter.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
         config.Formatters.Remove(config.Formatters.XmlFormatter);
      }
   }
}
```

Al final añadimos el formateador para CamelCase y quitamos el por defecto de Xml. Podemos añadir todos los formateadores que queramos en este punto.

Las rutas las indicaremos en los propios controladores usando un atributo de rutas y para ello las añadimos con la línea config.MapHttpAttributeRoutes();.

Seguido de estas rutas he añadido una ruta para controlar los errores 404 que es indispensable que vaya la última ya que el {url*} hace que se cojan todas las que aún no están definidas. Si te fijas en el proyecto de Api hay una carpeta “Controllers” con el controlador NotFound. Conceptualmente el proyecto Api corresponde a la configuración del Web Api y el Api.Core corresponde a los controladores con los endpoints. He añadido el controlador NotFound porque tengo una manía con esto de las APIs: no me gusta el mensaje que da microsoft por defecto en los 404. Es horroroso ya que nos manda una web junto con el código 404 por lo que me gusta sobrescribir ese comportamiento. Para ello el NotFoundController: 

```csharp
using System.Web.Http;
 
namespace Api.Controllers
{
   public class NotFoundController : ApiController
   {
      [HttpGet, HttpPost, HttpPut, HttpDelete, HttpHead, HttpOptions, AcceptVerbs("PATCH")]
      public IHttpActionResult ErrorNotFound()
      {
         return NotFound();
      }
   }
}
```

Es una manera un poco sucia de lograrlo, pero funciona. El controlador recoge todas las rutas que empiecen por la palabra “api” con “api/{*url}” y al tener todos los atributos de los verbos también los coge todos. Devuelve este NotFound(). Tengo que confesar que estas líneas se las he robado a mi compañero Eduardo Quintás que juntos nos hemos enfrentado a este mundo de las APIs en un par de ocasiones. Veremos en detalle tanto el funcionamiento de los verbos Http como el enrutado más adelante.

Al final el Controlador NotFound es configuración de cómo el Api devuelve las excepciones NotFound por lo que por eso es el único controlador que se mantiene en este proyecto.

Al principio inicializamos Autofac haciéndonos valer de la clase AutofacWebApiDependencyResolver:

```csharp
using Api.Controllers;
using Api.Core.Controllers.Base;
using Autofac;
using Autofac.Integration.WebApi;
using System.Net;
using System.Reflection;
using System.Web.Http;
 
namespace Api.App_Start
{
   public class DependencyContainer
   {
      public static IContainer BuildContainer(HttpConfiguration configuration)
      {
#if DEBUG
         ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) =&amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;amp;gt; true;
#endif
 
         var builder = new ContainerBuilder();
 
         builder.RegisterApiControllers(typeof(NotFoundController).GetTypeInfo().Assembly);
         builder.RegisterApiControllers(typeof(BaseController).GetTypeInfo().Assembly);
         builder.RegisterWebApiFilterProvider(configuration);
 
         // Register all services.
 
         var container = builder.Build();
         return container;
      }
   }
}
```

La línea que está en el if DEBUG es para que las APIs que usen https en modo debug no tengan que tener instalado el certificado para poder funcionar. Siempre es un poco rollo tener que estar con el certificado arriba y abajo y para desarrollo yo entiendo que no es necesario tenerlo. Donde sí es indispensable es en Producción. Me gusta poner esta línea aquí ya que la considero parte de nuestras dependencias.

Lo demás es nomenclatura de Autofac para añadir los controllers a partir de los assemblies tanto del NotFound como del proyecto de Api.Core dónde tenemos un controlador BaseController y la siguiente línea que nos permite registrar los filtros en la configuración del api para que él los añada automáticamente a nuestras dependencias. En el comentario de Register all services iremos poniendo nuestros servicios con sus interfaces correspondiente

Con esto acabamos el proyecto de Api.

## PROYECTO API.CORE

En este proyecto sólo tenemos que instalar como dependencia:

  * Microsoft.AspNet.WebApi.Client
  * Microsoft.AspNet.WebApi.Core
  * Newtonson.Json

Me gusta crear una clase estática Constansts para guardar las rutas del api aquí. También tengo la costumbre de ponerle un nombre al API que guardo en esta clase y que ya usamos anteriormente en la configuración de Swagger por lo que más o menos esta sería nuestra clase Constants:

```csharp
namespace Api.Core
{
   public static class Constants
   {
      public const string ApiName = "API";
 
      public const string BaseRoute = "api";
 
      public const string Test = "test";
      public const string GetTestBoolean = "get-test-boolean";
      public const string GetTestBooleanWithParam = "get-test-boolean-with-param/{id}";
      public const string PostTestBoolean = "post-test-boolean";
   }
}
```

### CONTROLADORES

BaseController es un controlador del cuál van a heredar todos los demás. Él será quien herede de ApiController. ApiController es la clase que nos proporciona WebApi para que hereden nuestros controladores. Tiene los métodos que van por debajo de lo que nosotros programamos para abstraernos del protocolo HTTP aunque casi todo es sobrescribible. Hablando con mis controladores estos años me he dado cuenta que siempre es útil tener un controller de herencia entre todos para reutilizar código. En este momento no hace falta una clase así; pero creo que mis controladores lo necesitarán en el futuro.

```csharp
using System.Web.Http;
 
namespace Api.Core.Controllers.Base
{
   public class BaseController : ApiController
   {
   }
}
```

El último archivo que he subido es un controlador de ejemplo llamada TestController, con el que voy a intentar explicar algunos conceptos sobre los Controladores: los objetos que crean nuestros endpoints. Cada función de cada uno de nuestros controladores será un endpoint de nuestro api. A continuación podéis ver la clase TestController:

```csharp
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
      public async Task&amp;amp;amp;amp;amp;lt;IHttpActionResult&amp;amp;amp;amp;amp;gt; GetTestBoolean()
      {
         return Ok(true);
      }
 
      [HttpGet]
      [ResponseType(typeof(bool))]
      [Route(Constants.GetTestBooleanWithParam)]
      public async Task&amp;amp;amp;amp;amp;lt;IHttpActionResult&amp;amp;amp;amp;amp;gt; GetTestBooleanWithParam(int id)
      {
         return Ok(true);
      }
 
      [HttpPost]
      [ResponseType(typeof(bool))]
      [Route(Constants.PostTestBoolean)]
      public async Task&amp;amp;amp;amp;amp;lt;IHttpActionResult&amp;amp;amp;amp;amp;gt; PostTestBoolean()
      {
         return Ok(true);
      }
   }
}
```
### VERBOS HTTP

Todos los endpoints de nuestra API tienen que tener un verbo HTTP. Como he mencionado anteriormente nunca hay que dejar de mirar a los servicios Http. Los principales son: GET, POST, PUT y DELETE.

  * **GET**: Lee la representación de un recurso. En un ámbito de base de datos nos devolvería el dato, equivalente a un select.
  * **POST**: Crea representaciones de recursos nuevos. Tiene contenido un BODY en el mensaje con información, normalmente de un formulario. En ámbito de base de datos sería equivalente a un insert.
  * **PUT**: Actualización de un recurso existente. En ámbito de base de datos realizar un Update.
  * **DELETE**: Borrado de un recurso. En ámbito de base de datos realizar un Delete.

Estos verbos los podemos colocar como atributos en los métodos de nuestros controladores. En este caso el método GetTestBoolean() tiene un atributo [HttpGet]. Pero también podemos usar [HttpPost], [HttpPut] o [HttpDelete]. Existe una nomenclatura en WebApi que por defecto coge los verbos de las acciones poniendo los nombres adecuados; pero a mí me gusta especificarlo. También se puede usar el atributo [AcceptVerbs(“Get”, “Post”)] para especificar que se cumplan varios pero esto no tiene mucho sentido ya que las llamadas deberían estar separadas.

Muchas veces es difícil encajar un Get o un Post; pero siempre hay que tener estas definiciones en cuenta para ver que casa mejor en nuestra llamada.

### DOCUMENTACIÓN DE LA RESPUESTA: ResponseType

Me gustaría darle valor al atributo **ResponseType**. Este atributo indica el valor devuelto por el método de nuestro api. Para mí es un valor crucial. Muchas veces nuestras APIs devuelven objetos dinámicos o creados al vuelo y no le damos importancia a documentar que es lo que devuelve nuestra API. Ya he hablado de ello y esto suele suceder cuando acoplamos nuestro cliente a la API y tendremos problemas si un segundo cliente, móvil, por ejemplo, quiere acceder al API ya que habrá que emplear tiempo en documentarla. El esfuerzo de poner este atributo a la hora de desarrollar el método en cuestión es mucho menor a si lo tenemos que hacer el día de mañana yendo método a método. ResponseType es un atributo que también usa Swagger para mostrarnos el tipo que devuelve la respuesta. Por tanto, siempre que podamos decoremos nuestros métodos con este atributo, aunque sea costoso mantenerlo.

### ENRUTADO DEL WEB API

A parte del tipo de petición http que tenemos hay que asignarle a cada método su url. Para ello nos vamos a apoyar en la clase estática Constants donde tenemos todos los valores de nuestras rutas.

#### ENRUTADO POR ATRIBUTO EN LOS CONTROLADORES

El atributo RoutePrefix se encarga de darle una ruta a todo el controlador. Este controlador estarán todas las direcciones que sean *http://nuestrapi.com/api/test*. A partir de esa ruta cada método debe tener su verbo correspondiente y su ruta con el atributo Route. **Route** se encarga de completar la ruta en este caso con el valor “get-test-boolean”. Por tanto la ruta de este método es *http://nuestrapi.com/api/test/get-test-boolean*.

La magia de todo esto se completa con la línea del WebApiConfig del proyecto de configuración del Api config.MapHttpAttributeRoutes();. Esta función busca todos los atributos Routes de los controladores que han sido registrados y genera todas las rutas.

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
   [Route(Constants.GetTestBoolean)]
   public async Task&amp;amp;amp;amp;amp;lt;IHttpActionResult&amp;amp;amp;amp;amp;gt; GetTestBoolean()
   {
      return Ok(true);
   }
}
```

#### ENRUTADO POR MAPHTTPROUTE EN LA CONFIGURACIÓN DEL WEBAPI

Hay otra manera de generar rutas en Web Api que es la que hemos usado en el NotFoundController:

```csharp
config.Routes.MapHttpRoute(
   name: "Error404",
   routeTemplate: "api/{*url}",
   defaults: new { controller = "NotFound", action = "ErrorNotFound" }
);
```

Aquí podemos indicarle un nombre a la ruta, una dirección o template y los valores por defecto. En este caso hay que indicarle controller y action para que sepa que controlador y método debe usar. El ejemplo típico de estos casos es el siguiente:

```csharp
config.Routes.MapHttpRoute(
   name: "DefaultApi",
   routeTemplate: "api/{controller}/{action}",
   defaults: new { id = RouteParameter.Optional },
   constraint: new { id = "\d+" }
);
```

En la url van las variables. {controller} se corresponde con el controler, {action} con la acción (método) del controller que corresponde e {id} es un parámetro del método. También se pueden poner variables opcionales como vemos en el ejemplo donde el id es opcional o restricciones, en este caso id debe ser un entero. Estos casos también se pueden reproducir en el atributo “test/get-boolean/{id = int}” donde ponemos un parámetro int que debe ser un entero. Hay muchos detalles que lo mejor es ir aprendiéndolos con la práctica.


#### Conclusiones del enrutado

Podéis visitar los enlaces siguientes para conocer más en profundidad los detalles:

  * https://www.asp.net/web-api/overview/web-api-routing-and-actions/routing-and-action-selection
  * https://www.asp.net/web-api/overview/web-api-routing-and-actions/attribute-routing-in-web-api-2

Mi preferencia es usar el atributo Route siempre que se pueda y dejar el otro tipo de rutas para casos específicos como el de NotFound.

### PARÁMETROS EN LOS CONTROLADORES

Para acabar esta primera sesión de programación de nuestra API me gustaría repasar la forma que tienen los parámetros de recibirlos en nuestros controladores. Os voy a contar cómo funcionan por defecto. Este comportamiento se puede cambiar sobrescribiendo el ModelBinder de Web Api, aunque desde mi experiencia pocas veces he cambiado la forma que se tiene por defecto.

Normalmente las peticiones HTTP tienen tres maneras de transportar información: en el QueryString, los parámetros de ruta o en el cuerpo de la petición:

  * **Parámetros de ruta**. Son los que van en la propia dirección
    * http://localhost/test/get-boolean/{id} => public async Task<IHttpActionResult> GetTestBoolean(int id)
    * http://localhost/test/{name}/{id}/{detail} => public async Task<IHttpActionResult> GetTestBoolean(string name, int id, string detail}
  * **Parámetros en el QueryString**
    * http://localhost/test/get-boolean?id=3 => public async Task<IHttpActionResult> GetTestBoolean(int id)
    * http://localhost/test/get-boolean?id=3&name=”angel” => public async Task<IHttpActionResult> GetTestBoolean(int id, string name}
  * **Cuerpo del mensaje**: cómo siempre indico hay que tener siempre presente HTTP. El funcionamiento de HTTP es que sólo hay un objeto en el cuerpo del mensaje, por lo que sólo podemos tener un objeto compuesto en nuestros parámetros. Este objeto puede contener a su vez lo que se quiera dentro ya que el cuerpo del mensaje de HTTP es un json. Este tipo de parámetro se suelen usar en POST y PUT ya que son los métodos que recogen la información de los formularios:
  
```csharp
public class MyObject
{
   public int Id { get; set; }
   public string Name { get; set; }
}
public async Task<IHttpActionResult> GetTestBoolean(MyObject object)
```
  
