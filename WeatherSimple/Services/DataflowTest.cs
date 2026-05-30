using System.Net.Http;
using System.Threading.Tasks;

namespace WeatherStation
{
    public class DataflowTest
    {
        string? apiKey = Environment.GetEnvironmentVariable("MY_API_KEY"); // Hardcoded API key in env file - modified real api key from Open weather
        string? notUsed = Environment.GetEnvironmentVariable("SOMETHING");
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> GetWeatherAsync(string city) // Testing with a hardcoded secret in the env file
        {
            string url =$"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric"; // Actually working website fetching weather forecast
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                
                var weather = new WeatherExtension();
                weather.useVariable(Environment.GetEnvironmentVariable(notUsed));

                return $"Weather data received successfully: \n\n{response}";

            }
            catch (Exception ex)
            {
                return $"Error retrieving weather data: {ex.Message}"; 
            }
            Console.WriteLine(apiKey);
        }
    }
}