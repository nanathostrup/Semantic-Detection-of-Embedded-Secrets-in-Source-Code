using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace WeatherStation
{
    public class WeatherExtension
    {
        public void useVariable(string secret)
        {
            // Console.WriteLine(secret); //used to test the dataflow analysis, if it can catch a use in another file
            // string defaultCity = "Amsterdam"; 
            string url2 =$"https://api.openweathermap.org/data/2.5/weather?appid={secret}&units=metric"; // Actually working website fetching weather forecast
            HttpClient _httpClient2 = new HttpClient();// to test and debug if httpDetector can find all instances of HttpClients
            var response = await _httpClient2.GetStringAsync(url2);
        }
    }
}