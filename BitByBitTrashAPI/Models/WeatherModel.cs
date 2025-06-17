namespace BitByBitTrashAPI.Models
{
    namespace BitByBitTrashAPI.Models
    {
        //zie response van de weather api           
        public class WeatherModel
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double GenerationtimeMs { get; set; }
            public int UtcOffsetSeconds { get; set; }
            public string Timezone { get; set; }
            public string TimezoneAbbreviation { get; set; }
            public double Elevation { get; set; }
            public CurrentUnitsModel CurrentUnits { get; set; }
            public CurrentWeatherModel Current { get; set; }
        }

        public class CurrentUnitsModel
        {
            public string Time { get; set; }
            public string Interval { get; set; }
            public string Temperature2m { get; set; }
            public string IsDay { get; set; }
        }

        public class CurrentWeatherModel
        {
            public string Time { get; set; }
            public int Interval { get; set; }
            public double Temperature2m { get; set; }
            public int IsDay { get; set; }
        }
    }

}
