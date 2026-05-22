namespace WeatherStation
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("=== Weather Station ===");

            var weatherService = new WeatherServices();

            Console.Write("Enter city: ");
            string? city = Console.ReadLine();
            if (city == null)
            {
                return;
            }

            string? weather = await weatherService.GetWeatherAsync(city);
            Console.WriteLine(weather);

            string? test = await weatherService.ALittleTest(city); //Bare for at teste når funktionen bliver kaldt
        }
    }
}
