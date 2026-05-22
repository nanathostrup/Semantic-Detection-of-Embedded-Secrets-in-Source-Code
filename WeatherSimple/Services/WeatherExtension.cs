using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace WeatherStation
{
    public class WeatherExtension
    {
        // private readonly HttpClient _httpClient1 = new HttpClient(); // to test and debug if httpDetector can find all instances of HttpClients

        public void useVariable(string secret)
        {
            Console.WriteLine(secret); //used to test the dataflow analysis, if it can catch a use in another file
            string defaultCity = "Amsterdam"; 
            // HttpClient _httpClient2 = new HttpClient();// to test and debug if httpDetector can find all instances of HttpClients
        }
    }
}