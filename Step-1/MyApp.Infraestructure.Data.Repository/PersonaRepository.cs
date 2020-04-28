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

        private readonly HttpClient _httpClient;
        private string _baseUri; 
        
        public PersonaRepository(IConfiguration configuration, HttpClient httpClient){
            ValidateConfiguration(configuration);
            _httpClient = httpClient;
        }

        private void ValidateConfiguration(IConfiguration configuration){
            if (configuration == null)
                throw new Exception(NoValidConfigurationMessage);            
            _baseUri = configuration[$"{ConfigurationSectionName}:{EndPointKey}"] ?? throw new Exception(NoEndPointMessage);
        }

        public IList<Persona> All(){
            try {
                var response = _httpClient.GetAsync(_baseUri);
                return null;
            }
            catch (Exception ex){                
                throw new Exception(AccessErrorServiceMessage, ex);
            }
        }
    }
}