using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace PropertyManagement.Services
{
    public class GoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GoogleMapsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleMaps:ApiKey"];
        }

        public async Task<string> GetAddressFromCoordinates(double latitude, double longitude)
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            var status = json["status"]?.ToString();
            if (status == "OK")
            {
                var address = json["results"]?[0]?["formatted_address"]?.ToString();
                return address;
            }

            return null;
        }

        public bool IsValidLatitude(double latitude) => latitude >= -90 && latitude <= 90;
        public bool IsValidLongitude(double longitude) => longitude >= -180 && longitude <= 180;
    }
}
