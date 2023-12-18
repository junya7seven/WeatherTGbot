using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherTGbot
{
     class GetDayTimer
    {
        private Timer timer;
        public void StartTimer(string apiKey, string city)
        {
            DateTime now = DateTime.Now;
            DateTime firstRunTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 3);

            if (now > firstRunTime)
            {
                firstRunTime = firstRunTime.AddMinutes(1);
            }

            TimeSpan interval = TimeSpan.FromSeconds(5); // Интервал 5 секунд
            timer = new Timer(async (_) => await WeatherProgram.GetWeatherDataAsync(city, apiKey), null, TimeSpan.Zero, interval);
            timer = new Timer(async (_) => await GetCurrency.GetCurrencyasync(), null, TimeSpan.Zero, interval);
        }
    }
}
