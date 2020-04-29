using System;
using Microsoft.Extensions.Configuration;
using Xunit;
using Moq;
using System.Net.Http;
using Moq.Protected;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Castle.DynamicProxy.Generators;
using System.IO;
using System.Dynamic;

namespace MyApp.Infraestructure.Data.Repository.Test {

    public class PersonaRepositoryTest {
        
        private readonly string _configurationKey = $"{PersonaRepository.ConfigurationSectionName}:{PersonaRepository.EndPointKey}";
        
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

        private IConfiguration MockConfigurationObject( string base_url ){
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(cfg => cfg[_configurationKey])
                    .Returns(base_url);
            return configurationMock.Object;
        }

        [Fact]
        public void DeberiaDefinirUnErrorGenericoDeAccesoAlServicio()
        {
            Assert.Equal("Servicio personas no disponible", PersonaRepository.AccessErrorServiceMessage);
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

        [Fact]
        public void DeberiaMostrarElMensajeDeErrorGenericoSiNoEncuentraElRecurso()
        {
            const string incorrectUri = "http://unknowhost";
            var configuration = MockConfigurationObject(incorrectUri);
            var notFoundResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
            var mockMessageHandler = MockMessageHandler(notFoundResponseMessage);
            var httpClient = new HttpClient(mockMessageHandler.Object);
            var sut = new PersonaRepository(configuration, httpClient);

            var exception = Assert.Throws<Exception>(() => sut.All());

            Assert.Equal(PersonaRepository.AccessErrorServiceMessage, exception.Message);
        }

        private static Mock<HttpMessageHandler> MockMessageHandler(HttpResponseMessage notFoundResponseMessage)
        {
            var mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync"
                    , ItExpr.IsAny<HttpRequestMessage>()
                    , ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(notFoundResponseMessage);
            return mockMessageHandler;
        }

        [Fact]
        public void DeberiaInvocarAlRecursoCorrecto()
        {
            var baseUri = "http://unknowhost/api-personas/1.0/";
            var completeUri = baseUri + "personas";
            var configuration = MockConfigurationObject(baseUri);
            var okResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var mockMessageHandler = MockMessageHandler(okResponse);
            var httpClient = new HttpClient(mockMessageHandler.Object);
            var sut = new PersonaRepository(configuration, httpClient);

            var personas = sut.All();

            mockMessageHandler.Protected().Verify("SendAsync"
                , Times.Once()
                , ItExpr.Is<HttpRequestMessage>( r => r.RequestUri.AbsoluteUri.Equals(completeUri) )
                , ItExpr.IsAny<CancellationToken>());
        }
        
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
            var mockMessageHandler = MockMessageHandler(emptyArrayResponseMessage);
            var httpClient = new HttpClient(mockMessageHandler.Object);
            var sut = new PersonaRepository(configuration, httpClient);

            var personas = sut.All();

            Assert.Empty(personas);
        }

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
    }
}