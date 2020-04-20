using System;
using MyApp.Domain.Entities;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace MyApp.Infraestructure.Data.Repository
{
    public class PersonaRepository : IRepository<Persona>
    {
        public static readonly string ConfigurationSectionName = "repository_apis";
        public static readonly string EndPointKey = "url_base_personas";

        public static readonly string NoValidConfigurationMessage = "No se informó la configuración";        
        public static readonly string NoConfigurationSectionMessage = $"No se encuentra la sección de configuración '{ConfigurationSectionName}'";        
        public static readonly string NoEndPointMessage = $"La sección {ConfigurationSectionName} no contiene '{EndPointKey}'";
        public static readonly string AccessErrorServiceMessage = "Servicio personas no disponible";
        
        public PersonaRepository(IConfiguration configuration, HttpClient httpClient){
            ValidateConfiguration(configuration);
        }

        private void ValidateConfiguration(IConfiguration configuration){
            if (configuration == null)
                throw new Exception(NoValidConfigurationMessage);
            
            var configurationSection = configuration.GetSection(ConfigurationSectionName) ?? throw new Exception(NoConfigurationSectionMessage);
            
            if ( configurationSection[EndPointKey] == null )
                throw new Exception(NoEndPointMessage);
        }

        public IList<Persona> All(){
            throw new NotImplementedException();
        }
    }
}