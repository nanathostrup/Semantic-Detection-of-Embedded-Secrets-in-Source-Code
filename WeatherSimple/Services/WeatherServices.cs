using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WeatherStation
{
    public class WeatherServices
    {
        string? apiKey = Environment.GetEnvironmentVariable("MY_API_KEY"); // Hardcoded API key in env file - modified real api key from Open weather
        string? apiKey1 = Environment.GetEnvironmentVariable("JWT_SECRET"); // Hardcoded API key in env file - real(ish) jwt used as example in official debugger page: https://www.jwt.io/
        string? defaultCity = Environment.GetEnvironmentVariable("WEATHER_DEFAULT_CITY"); // Non-secret - Test to differntiate between actual secret and non secret
        string? unusedJWTKey = Environment.GetEnvironmentVariable("JWT_ONLYCALLED_SECRET");  // Almost the same JWT secret as before, only this is not going to be called in the code only extracted from env file. Modified example in official debugger page: https://www.jwt.io/ 
        var weatherExtension = new WeatherExtension();

        private readonly HttpClient _httpClient = new HttpClient();
        public async Task<string> GetWeatherAsync(string city) // Testing with a hardcoded secret in the env file
        {
            city ??= defaultCity;
            // weatherExtension.useVariable(apiKey);
            string password = "sk_live_hhGYgz9xNy6BGqzVmhDnrYTFcmTsw8A9mzWDyupL5ZyuJ80nK2JzPFzwVeFUGVg3";

            Console.WriteLine(Environment.GetEnvironmentVariable("HEJ"));
            //WeatherExtension.useVariable(Environment.GetEnvironmentVariable("HEJ")); //Test that walker gathers this even though it is not assigned just used.

            int random = 1; // Dataflow analyse skal ikke røre den her!

            string? newapikey = apiKey1; //For at teste dataflow analysen om den samler den her variabel op

            //string? newapikey1 = newapikey; //sæt den ned i url for at teste dataflow analyse

            string url =
             $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={newapikey}&units=metric"; // Actually working website fetching weather forecast
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                return $"Weather data received successfully: \n\n{response}";
            }
            catch (Exception ex)
            {
                return $"Error retrieving weather data: {ex.Message}"; 
            }
            Console.WriteLine(random); // for at teste dataflow analyse ikke rører random
            
            Action a = Console.WriteLine;
        }
    }
}