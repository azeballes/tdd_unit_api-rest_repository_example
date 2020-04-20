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
    public void RepositorioPersonasNecesitaAccederALaConfiguración(){
        IConfiguration configuration = null;
        var exception = Assert.Throws<Exception>( () => new PersonaRepository(configuration) );
        Assert.Equal(PersonaRepository.NoValidConfigurationMessage, exception.Message);
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
*Se definen algunas constantes en relación a los mensajes de error para las cuales no se escribió la prueba. Pero en el sentido estricto de la práctica debería haber un test que pruebe que la clase tiene esas constantes :)*

**6- Segundo test**

En nuestro segundo test vamos a pasar una colaboración válida de configuración y debemos validar que la misma contenga la sección de configuración correspondiente. En caso de que la sección no exista devolvemos el mensaje asociado.

Debido a estamos utilizando pruebas unitarias sólo debemos probar la clase que estamos construyendo y no sus colaboraciones. Para ello vamos a utilizar Moq para instanciar mocks de las dependencias.

> dotnet add MyApp.Infraestructure.Data.Repository.Test package Moq

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void RepositorioPersonasNecesitaAccederALaSecciónDeConfiguración(){
        IConfiguration configuration = new Mock<IConfiguration>().Object;
        var exception = Assert.Throws<Exception>( () => new PersonaRepository(configuration) );
        Assert.Equal(PersonaRepository.NoConfigurationSectionMessage, exception.Message);
    }
```
> dotnet add MyApp.Infraestructure.Data.Repository package Microsoft.Extensions.Configuration

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public class PersonaRepository : IRepository<Persona>
    {
        public static readonly string NoValidConfigurationMessage = "No se informó la configuración";
        private static readonly string ConfigurationSectionName = "repository_apis";
        public static readonly string NoConfigurationSectionMessage = $"No se encuentra la sección de configuración '{ConfigurationSectionName}'";
```

Hacemos pasar el test mediante la implementación más simple

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public PersonaRepository(IConfiguration configuration){
        throw new Exception(NoValidConfigurationMessage);
    }
```

Hacemos pasar el test

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public PersonaRepository(IConfiguration configuration){
        if (configuration == null)
            throw new Exception(NoValidConfigurationMessage);
        else
            throw new Exception(NoConfigurationSectionMessage);
    }
```

**7- Tercer test**

Ahora vamos a validar que la sección de configuración contenga la propiedad con la base del recurso de la api. Al igual que en los casos anteriores, en caso de que no exista devolvemos el mensaje asociado.

> MyApp.Infraestructure.Data.Repository.Test.PersonaRepositoryTest.cs:
```csharp
    [Fact]
    public void RepositorioPersonasNecesitaAccederALaDirecciónBaseDelRecurso(){
        var configurationMock = new Mock<IConfiguration>();
        var configurationSectionMock = new Mock<IConfigurationSection>();
        configurationMock.Setup(cfg => cfg.GetSection(PersonaRepository.ConfigurationSectionName))
                .Returns(configurationSectionMock.Object);
        var exception = Assert.Throws<Exception>( () => new PersonaRepository(configurationMock.Object) );
        Assert.Equal(PersonaRepository.NoEndPointMessage, exception.Message);
    }
```
> dotnet add MyApp.Infraestructure.Data.Repository package Microsoft.Extensions.Configuration

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public class PersonaRepository : IRepository<Persona>
    {
        public static readonly string ConfigurationSectionName = "repository_apis";
        private static readonly string EndPointKey = "url_base_personas";

        public static readonly string NoValidConfigurationMessage = "No se informó la configuración";        
        public static readonly string NoConfigurationSectionMessage = $"No se encuentra la sección de configuración '{ConfigurationSectionName}'";        
        public static readonly string NoEndPointMessage = $"La sección {ConfigurationSectionName} no contiene '{EndPointKey}'";
```

Hacemos pasar el test mediante la implementación más simple

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public PersonaRepository(IConfiguration configuration){
        ValidateConfiguration(configuration);
    }

    private void ValidateConfiguration(IConfiguration configuration){
        if (configuration == null)
            throw new Exception(NoValidConfigurationMessage);
        
        var configurationSection = configuration.GetSection(ConfigurationSectionName) ?? throw new Exception(NoConfigurationSectionMessage);
        
        if ( configurationSection[EndPointKey] == null )
            throw new Exception(NoEndPointMessage);
    }
```

Hacemos pasar el test

> MyApp.Infraestructure.Data.Repository.PersonaRepository.cs:
```csharp
    public PersonaRepository(IConfiguration configuration){
        if (configuration == null)
            throw new Exception(NoValidConfigurationMessage);
        else
            throw new Exception(NoConfigurationSectionMessage);
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
    private IConfiguration MockConfigurationObject( string base_url ){
        var configurationMock = new Mock<IConfiguration>();
        var configurationSectionMock = new Mock<IConfigurationSection>();
        configurationSectionMock.Setup( section => section[PersonaRepository.EndPointKey] ).Returns(base_url);
        configurationMock.Setup(cfg => cfg.GetSection(PersonaRepository.ConfigurationSectionName))
                .Returns(configurationSectionMock.Object);
        return configurationMock.Object;
    }

    [Fact]
    public void ObtenerTodasLasPersonasDeberíaFallarSiNoTieneUnaDirecciónBaseVálida(){
        const string noValidEnpoint = "";
        var configuration = MockConfigurationObject(noValidEnpoint);
        var mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(mockMessageHandler.Object);
        var sut = new PersonaRepository(configuration, httpClient);
        
        var exception = Assert.Throws<Exception>( () => sut.All() );
        
        Assert.Equal(PersonaRepository.AccessErrorServiceMessage, exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }
```

## Continuará ...