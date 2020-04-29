## Introducción

La idea es construir un simple ejemplo de acceso a una api rest a través del patrón repositorio mediante tdd con net core, xunit y moq.

La api de ejemplo se encuentra disponible en 

https://app.swaggerhub.com/apis-docs/azeballes/api-personas/1.0.0

La configuración de acceso a la api rest vamos a suponer que la tendremos disponible en la configuración de la app:

> appsettings.json:
```json
{
    "repository_apis" : {
        "url_base_personas" : "https://app.swaggerhub.com/apis-docs/azeballes/api-personas/1.0.0"
    }
}
```

### Arquitectura de aplicación

Utilizando DDD vamos a crear la siguiente arquitectura de aplicación:

MyApp.Infraestructure.Data.Repository
MyApp.Infraestructure.Data.Repository.Test
MyApp.Domain.Entities

**1- Creamos la configuración inicial de la app**

dotnet new sln --name MyApp
dotnet new classlib --name MyApp.Domain.Entities
dotnet new classlib --name MyApp.Infraestructure.Data.Repository
dotnet new xunit --name MyApp.Infraestructure.Data.Repository.Test

dotnet sln add MyApp.Domain.Entities MyApp.Infraestructure.Data.Repository MyApp.Infraestructure.Data.Repository.Test

dotnet add MyApp.Infraestructure.Data.Repository reference MyApp.Domain.Entities
dotnet add MyApp.Infraestructure.Data.Repository.Test reference MyApp.Domain.Entities MyApp.Infraestructure.Data.Repository

**2- Ejecutamos la prueba por defecto para verificar que todo esté correcto**

dotnet test --no-restore

**3- Agregamos una interfaz muy simple del patrón repositorio en MyApp.Infraestructure.Data.Repository**

> MyApp.Infraestructure.Data.Repository.IRepository.cs:
```csharp
using System.Collections.Generic;

namespace MyApp.Infraestructure.Data.Repository
{
    public interface IRepository<T> where T: class
    {
        IList<T> All();
    }
}
```
**4- Agregamos nuestra clase de test**

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
using System;
using Xunit;

namespace MyApp.Infraestructure.Data.Repository.Test
{
    public class PersonaRepositoryTest
    {
    }
}
```

### Pruebas de inicialización

**5- Primer test**

Nuestro repositorio de acceso a la api rest de personas necesita acceder inicialmente a la configuración para poder obtener la url base del recurso. Nuestro primer test verifica que si no indico un colaborador válido (dependencia) de configuración en el constructor obtenga un mensaje de error con la descripción correspondiente.

> dotnet add MyApp.Infraestructure.Data.Repository.Test package Microsoft.Extensions.Configuration

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void DeberiaDefinirUnMensajeDeConfiguracionNoValida()
    {
        Assert.Equal("No se informó la configuración", PersonaRepository.NoValidConfigurationMessage);
    }

    [Fact]
    public void DeberiaFallarSiNoSeInformaLaConfiguracion() {
        IConfiguration configuration = null;
        var exception = Assert.Throws<Exception> (() => new PersonaRepository (configuration, null));
        Assert.Equal (PersonaRepository.NoValidConfigurationMessage, exception.Message);
    }
```

> dotnet add MyApp.Infraestructure.Data.Repository package Microsoft.Extensions.Configuration

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
using System;
using MyApp.Domain.Entities;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace MyApp.Infraestructure.Data.Repository
{
    public class PersonaRepository : IRepository<Persona>
    {
        public static readonly string NoValidConfigurationMessage = "No se informó la configuración";

        public PersonaRepository(IConfiguration configuration){

        }

        public IList<Persona> All(){
            throw new NotImplementedException();
        }
    }
}
```

> MyApp.Domain.Entities.Persona.cs:
```csharp
namespace MyApp.Domain.Entities
{
    public class Persona
    {
        
    }
}
```
Hacemos pasar el test mediante la imple más simple

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public PersonaRepository(IConfiguration configuration){
        throw new Exception(NoValidConfigurationMessage);
    }
```

**6- Segundo test**

En nuestro segundo test vamos a pasar una colaboración válida de configuración y debemos validar que la misma contenga la configuración correspondiente. En caso de no encontrarse la configuración devolvemos el mensaje asociado.

Debido a estamos utilizando pruebas unitarias sólo debemos probar la clase que estamos construyendo y no sus colaboraciones. Para ello vamos a utilizar Moq para instanciar mocks de las dependencias.

> dotnet add MyApp.Infraestructure.Data.Repository.Test package Moq

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void DeberiaDefinirElNombreDeLaSeccionDeConfiguracion()
    {
        Assert.Equal("repository_apis", PersonaRepository.ConfigurationSectionName);            
    }

    [Fact]
    public void DeberiaDefinirElNombreDeLaPropiedadDireccionBaseDelRecurso()
    {
        Assert.Equal("url_base_personas", PersonaRepository.EndPointKey);
    }

    [Fact]
    public void DeberiaFallarSiNoSeConfiguranLasPropiedadesNecesarias(){
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(cfg => cfg[_configurationKey])
                .Returns<IConfiguration>(null);
        var exception = Assert.Throws<Exception>( () => new PersonaRepository(configurationMock.Object, null) );
        Assert.Equal(PersonaRepository.NoEndPointMessage, exception.Message);
    }
```
> dotnet add MyApp.Infraestructure.Data.Repository package Microsoft.Extensions.Configuration

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public class PersonaRepository : IRepository<Persona>
    {
        public static readonly string ConfigurationSectionName = "repository_apis";
        public static readonly string EndPointKey = "url_base_personas";        
        public static readonly string NoEndPointMessage = $"La sección {ConfigurationSectionName} no contiene '{EndPointKey}'";
```

Hacemos pasar el test

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public PersonaRepository(IConfiguration configuration){
        if (configuration == null)
            throw new Exception(NoValidConfigurationMessage);
        throw new Exception(NoEndPointMessage);
    }
```

**7- Tercer test**

Finalmente en este ciclo de pruebas de inicialización verificaremos que cuando se pasa una configuración correcta se puede inicializar el repositorio correctamente.

Ante la ausencia de la aserción _DoesNotThrows_ en xUnit escribimos el test de la siguiente forma:

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    
    private readonly string _configurationKey = $"{PersonaRepository.ConfigurationSectionName}:{PersonaRepository.EndPointKey}";
    
    [Fact]
    public void DeberiaPoderInstanciarseCuandoLaConfiguracionEsCorrecta()
    {
        const string validUri = "http://mydomain.com";
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(cfg => cfg[_configurationKey])
                .Returns(validUri);
        bool allRight = false;
        try
        {
            new PersonaRepository(configurationMock.Object, null);
            allRight = true;
        }
        catch(Exception ex)
        {
            allRight = false;
        }
        Assert.True(allRight);
    }
```

Hacemos pasar el test de la siguiente forma:

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    private void ValidateConfiguration(IConfiguration configuration){
        if (configuration == null)
            throw new Exception(NoValidConfigurationMessage);            
        _baseUri = configuration[$"{ConfigurationSectionName}:{EndPointKey}"] ?? throw new Exception(NoEndPointMessage);
    }
```

## Listar personas

**8- Cuarto test**

Para poder acceder a un recurso en una api rest necesitamos la dirección base del recurso. De acuerdo a lo anterior en este nuevo test vamos a configurar una dirección base incorrecta y deberíamos obtener una falla al intentar acceder al recurso.

Inicialmente vamos a agregar la colaboración de un HttpClient al repositorio

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public PersonaRepository(IConfiguration configuration, HttpClient httpClient){
        ValidateConfiguration(configuration);
    }
```

*Refactorizamos los tests agregando null a la instancia del repo*

En este cuarto test vamos a efectuar la prueba más simple de falla del servicio externo debido a una configuración no válida del recurso base.
El test verifica que se genere una excepción con un mensaje amigable y que la misma tenga una excepción anidada del tipo InvalidOperationException (que se produce cuando se indica una uri incorrecta) 


> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void DeberiaDefinirUnErrorGenericoDeAccesoAlServicio()
    {
        Assert.Equal("Servicio personas no disponible", PersonaRepository.AccessErrorServiceMessage);
    }
    
    private IConfiguration MockConfigurationObject( string base_url ){
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(cfg => cfg[_configurationKey])
                    .Returns(base_url);
            return configurationMock.Object;
    }    

    [Fact]
    public void DeberiaMostrarElMensajeDeErrorGenericoSiNoPuedeAccederAlServicio(){
        const string invalidUri = "";
        var configuration = MockConfigurationObject(invalidUri);
        var mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(mockMessageHandler.Object);
        var sut = new PersonaRepository(configuration, httpClient);
            
        var exception = Assert.Throws<Exception>( () => sut.All() );            
        Assert.Equal(PersonaRepository.AccessErrorServiceMessage, exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }
```
Para hacer pasar el test podemos hacer:

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:

```csharp
    public IList<Persona> All(){
        try {
            var response = _httpClient.GetAsync(_baseUri);
            return null;
        }
        catch (Exception ex){                
            throw new Exception(AccessErrorServiceMessage, ex);
        }
    }
```

**9- Quinto test**

En este test vamos a simular una configuración incorrecta de la url base del recurso. De acuerdo a lo anterior cuando se intente acceder a la api se debería retornar un HTTP 404 de recurso no encontrado.
En este caso como no es un código de respuesta válido de la api lo vamos a modelar como un error genérico de acceso al servicio.

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void DeberiaMostrarElMensajeDeErrorGenericoSiNoEncuentraElRecurso()
    {
        const string incorrectUri = "http://unknowhost";
        var configuration = MockConfigurationObject(incorrectUri);
        var notFoundResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
        var mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>() )
            .ReturnsAsync(notFoundResponseMessage);
        var httpClient = new HttpClient(mockMessageHandler.Object);
        var sut = new PersonaRepository(configuration, httpClient);

        var exception = Assert.Throws<Exception>(() => sut.All());
            
        Assert.Equal(PersonaRepository.AccessErrorServiceMessage, exception.Message);            
    }
```
Modificamos la implementación para hacer pasar el test
> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public IList<Persona> All(){
        try {
            var response = _httpClient.GetAsync(_baseUri);
            throw new Exception(AccessErrorServiceMessage);
        }
        catch (Exception ex){                
            throw new Exception(AccessErrorServiceMessage, ex);
        }
    }
```

**10- Sexto test**
El siguiente paso sería asegurarnos que nuestro repositorio invoque al recurso correcto utilizando la dirección base configurada. Para ello utilizamos el siguiente test:
> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void DeberiaInvocarAlRecursoCorrecto()
    {
        var baseUri = "http://unknowhost/api-personas/1.0/";
        var completeUri = baseUri + "personas";
        var configuration = MockConfigurationObject(baseUri);
        var okResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync"
                , ItExpr.IsAny<HttpRequestMessage>()
                , ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(okResponse);
        var httpClient = new HttpClient(mockMessageHandler.Object);
        var sut = new PersonaRepository(configuration, httpClient);

        var personas = sut.All();

        mockMessageHandler.Protected().Verify("SendAsync"
            , Times.Once()
            , ItExpr.Is<HttpRequestMessage>( r => r.RequestUri.AbsoluteUri.Equals(completeUri) )
            , ItExpr.IsAny<CancellationToken>());
    }
```

Para poder pasar este test debemos modificar la implementación por ejemplo de la siguiente forma:

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public IList<Persona> All()
    {
        try
        {
            var uri = Path.Combine(_baseUri, "personas");
            var response = _httpClient.GetAsync(uri);
            if (response.Result.StatusCode != HttpStatusCode.OK)
                throw new Exception(AccessErrorServiceMessage);
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception(AccessErrorServiceMessage, ex);
        }
    }
```
_Con Path.Combine nos aseguramos de que no tengamos problemas en relación al separador de paths en la url_

**11- Septimo test**

En el siguiente test vamos a simular una respuesta correcta desde la api, sin personas disponibles en la respuesta, con lo cual deberíamos obtener una lista vacía de personas.

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void DeberiaRetornarUnaListaVaciaDePersonas()
    {
        const string baseUri = "http://unknowhost/api-personas/1.0";
        var configuration = MockConfigurationObject(baseUri);
        var emptyArrayResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"{""cantidad"":0,""personas"":[]}")

        };            
        var mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync"
                , ItExpr.IsAny<HttpRequestMessage>()
                , ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(emptyArrayResponseMessage);
        var httpClient = new HttpClient(mockMessageHandler.Object);
        var sut = new PersonaRepository(configuration, httpClient);

        var personas = sut.All();

        Assert.Empty(personas);
    }    
```

Una de las implementaciones posibles podría ser:

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public IList<Persona> All()
    {
        try
        {
            var uri = Path.Combine(_baseUri, "personas");
            var response = _httpClient.GetAsync(uri);
            if (response.Result.StatusCode != HttpStatusCode.OK)
                throw new Exception(AccessErrorServiceMessage);
            return new List<Persona>();
        }
        catch (Exception ex)
        {
            throw new Exception(AccessErrorServiceMessage, ex);
        }
    }
```

**12- Octavo test**

Ahora vamos a probar que la respuesta desde la api contenga una persona. En tal deberíamos validar que la persona sea la devuelta por el servicio.

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void DeberiaRetornarUnaPersona()
    {
        const string baseUri = "http://unknowhost/api-personas/1.0";
        var configuration = MockConfigurationObject(baseUri);

        dynamic persona = new ExpandoObject();
        persona.id = 10;
        persona.nombre = "Juan";
        persona.apellido = "Perez";
        persona.fecha_nacimiento = new DateTime(1980, 01, 02);
            
        var onePersonResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent($@"{{
                ""cantidad"" : 1,
                ""personas"" : [
                    {{
                        ""id"" : {persona.id},
                        ""nombre"" : ""{persona.nombre}"",
                        ""apellido"" : ""{persona.apellido}"",
                        ""fecha_nacimiento"" : ""{persona.fecha_nacimiento:""yyyy-MM-dd""}""
                    }}
                ]}}")
        };
            
        var mockMessageHandler = MockMessageHandler(onePersonResponse);
        var httpClient = new HttpClient(mockMessageHandler.Object);
        var sut = new PersonaRepository(configuration, httpClient);

        var personas = sut.All();

        Assert.Equal(persona.id, personas[0].Id);
        Assert.Equal(persona.nombre, personas[0].Nombre);
        Assert.Equal(persona.apellido, personas[0].Apellido);
        Assert.Equal(persona.fecha_nacimiento, personas[0].FechaNacimiento);
    }    
```

Podemos hacer pasar el test de la siguiente forma:

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    
```