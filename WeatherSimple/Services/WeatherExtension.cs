using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace WeatherStation
{
    public class WeatherExtension
    {
        // private readonly HttpClient _httpClient1 = new HttpClient(); // to test and debug if httpDetector can find all instances of HttpClients

        // private readonly HttpClient _httpClient1 = new HttpClient();

        public void useVariable(string secret)
        {
            Console.WriteLine(secret); //used to test the dataflow analysis, if it can catch a use in another file
            string defaultCity = "Amsterdam"; 
            // HttpClient _httpClient2 = new HttpClient();// to test and debug if httpDetector can find all instances of HttpClients
        }
        // public async void dataFlowTest()
        // {
        //     int random = 1;
        //     // var response = await _httpClient1.GetStringAsync($"https://api.openweathermap.org/data/2.5/weather?q={random}");
        // }
    }
}