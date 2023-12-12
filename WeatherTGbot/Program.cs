using Newtonsoft.Json;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

var botClient = new TelegramBotClient("Your_Api");
var apiKey = "Your_Api"; // Обновите ключ API здесь
string city = "Набережные Челны";

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

WeatherProgram weatherProgram = new WeatherProgram();
weatherProgram.StartTimer(city, apiKey);
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
    try
    {
        string weatherMessage =  await WeatherProgram.GetWeatherDataAsync(city, apiKey);

        if (messageText.StartsWith("/weather"))
        { 
            // Send the message to the user
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: weatherMessage,
                cancellationToken: cancellationToken);
        }
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

