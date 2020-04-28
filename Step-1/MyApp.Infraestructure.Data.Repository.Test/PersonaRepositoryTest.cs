using System;
using Microsoft.Extensions.Configuration;
using Xunit;
using Moq;
using System.Net.Http;

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
            const string noValidUri = "";
            var configuration = MockConfigurationObject(noValidUri);
            var mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(mockMessageHandler.Object);
            var sut = new PersonaRepository(configuration, httpClient);
            
            var exception = Assert.Throws<Exception>( () => sut.All() );            
            Assert.Equal(PersonaRepository.AccessErrorServiceMessage, exception.Message);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }
        
    }
}