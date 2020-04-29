using System;
using MyApp.Domain.Entities;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using System.IO;

namespace MyApp.Infraestructure.Data.Repository
{
    public class PersonaRepository : IRepository<Persona>
    {
        public static readonly string ConfigurationSectionName = "repository_apis";
        public static readonly string EndPointKey = "url_base_personas";

        public static readonly string NoValidConfigurationMessage = "No se informó la configuración";        
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

        public IList<Persona> All()
        {
            try
            {
                var uri = Path.Combine(_baseUri, "personas");
                var response = _httpClient.GetAsync(uri);
                if (response.Result.StatusCode != HttpStatusCode.OK)
                    throw new Exception(AccessErrorServiceMessage);
                //return ParseResponse(response.Result.Content.ReadAsStringAsync().Result);
                return new List<Persona>();
            }
            catch (Exception ex)
            {
                throw new Exception(AccessErrorServiceMessage, ex);
            }
        }
        /*
        private IList<Persona> ParseResponse(string result)
        {
            return new List<Persona>();
        }
        */
    }
}