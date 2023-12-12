using Newtonsoft.Json;

public class WeatherProgram
{
    private Timer timer;
    public void StartTimer(string apiKey,string city)
    {
        DateTime now = DateTime.Now;
        DateTime firstRunTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 3);

        if (now > firstRunTime)
        {
            firstRunTime = firstRunTime.AddMinutes(1);
        }

        TimeSpan interval = TimeSpan.FromSeconds(5); // Интервал 5 секунд
        timer = new Timer(async (_) => await GetWeatherDataAsync(city, apiKey), null, TimeSpan.Zero, interval);
    }
    // Метод для получения данных о погоде
    public static async Task<string> GetWeatherDataAsync(string city, string apiKey)
    {
        try
        {
            // Формируем URL запроса
            var url = $"http://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&lang=ru";

            // Отправляем запрос и получаем ответ
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    // Анализируем ответ
                    var json = await response.Content.ReadAsStringAsync();
                    var weatherData = JsonConvert.DeserializeObject<WeatherData>(json);

                    // Создаем сообщение о погоде
                    string weatherMessage = $"Город: {weatherData.name}\n" +
                                            $"Температура: {Math.Round(weatherData.main.temp - 273.15, 0)}°C\n" +
                                            $"Погода: {weatherData.weather[0].description}\n" +
                                            $"Ощущается как: {Math.Round(weatherData.main.feels_like - 273.15, 0)}°C";

                    return weatherMessage;
                }
                else
                {
                    // Если ответ неуспешный, возвращаем сообщение об ошибке
                    return $"Ошибка при получении данных о погоде. Код состояния: {response.StatusCode}";
                }
            }
        }
        catch (Exception ex)
        {
            // Если произошла ошибка при выполнении запроса, возвращаем сообщение об ошибке
            return $"Произошла ошибка при получении данных о погоде: {ex.Message}";
        }
    }

}