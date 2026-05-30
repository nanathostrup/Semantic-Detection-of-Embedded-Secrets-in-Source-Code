using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace WeatherStation
{
    public class WeatherExtension
    {
        public void sendOn()
        {
            WeatherExtension weather = new WeatherExtension();
            string secret1 = "Hello";
            weather.useVariable(secret1);
        }
    }
}