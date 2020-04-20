using System;
using Microsoft.Extensions.Configuration;
using Xunit;
using Moq;
using System.Net.Http;

namespace MyApp.Infraestructure.Data.Repository.Test {

    public class PersonaRepositoryTest {
        [Fact]
        public void RepositorioPersonasNecesitaAccederALaConfiguración () {
            IConfiguration configuration = null;
            var exception = Assert.Throws<Exception> (() => new PersonaRepository (configuration, null));
            Assert.Equal (PersonaRepository.NoValidConfigurationMessage, exception.Message);
        }

        [Fact]
        public void RepositorioPersonasNecesitaAccederALaSecciónDeConfiguración(){
            IConfiguration configuration = new Mock<IConfiguration>().Object;
            var exception = Assert.Throws<Exception>( () => new PersonaRepository(configuration, null) );
            Assert.Equal(PersonaRepository.NoConfigurationSectionMessage, exception.Message);
        }

        [Fact]
        public void RepositorioPersonasNecesitaAccederALaDirecciónBaseDelRecurso(){
            var configurationMock = new Mock<IConfiguration>();
            var configurationSectionMock = new Mock<IConfigurationSection>();
            configurationMock.Setup(cfg => cfg.GetSection(PersonaRepository.ConfigurationSectionName))
                    .Returns(configurationSectionMock.Object);
            var exception = Assert.Throws<Exception>( () => new PersonaRepository(configurationMock.Object, null) );
            Assert.Equal(PersonaRepository.NoEndPointMessage, exception.Message);
        }

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
            Assert.NotNull(exception.InnerException.Message);
        }
        
    }
}