using Newtonsoft.Json;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;

var botClient = new TelegramBotClient("Your_API Telegram");
var apiKey = "Your_API OpenWeather"; // Обновите ключ API здесь
string city = "Moscow"; // Город
using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    // Echo received message text
    try
    {
        // Send the weather data to the user
        WeatherData weatherData = await WeatherProgram.GetWeatherDataAsync(city, apiKey);

        // Create a message with the weather information
        string weatherMessage = $"Город: {weatherData.name}\n" +
                                $"Температура: {Math.Round(weatherData.main.temp - 273.15, 0)}°C\n" +
                                $"Погода: {weatherData.weather[0].description}\n" +
                                $"Ощущается как: {Math.Round(weatherData.main.feels_like - 273.15, 0)}°C";

        // Send the message to the user
        Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: weatherMessage,
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        // Send an error message to the user if an exception occurs
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"Произошла ошибка при получении данных о погоде: {ex.Message}",
            cancellationToken: cancellationToken);
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}



class WeatherProgram
{
    public static async Task<string> HttpRequestAsync(string uri)
    {
        using (var httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception("HTTP Request failed.");
            }
        }
    }

    public static async Task<WeatherData> GetWeatherDataAsync(string city, string apiKey)
    {
        string uri = $"http://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}";
        string response = await HttpRequestAsync(uri);
        var weatherData = JsonConvert.DeserializeObject<WeatherData>(response);
        return weatherData;
    }
}
