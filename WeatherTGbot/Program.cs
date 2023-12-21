using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Net;
using WeatherTGbot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

var botClient = new TelegramBotClient("6948054071:AAHswM8cDEpKQ4y18Byj7iwKM_DbV5a0C-M");
var apiKey = "908bf0611586f5cbdefb8540ccf08a54"; // Обновите ключ API здесь
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


// Остальной код вашего приложения Telegram Bot остается таким же, как ранее.

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message || message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;
    using (var dbContext = new BotDbContext())
    {

        var userChat = new UserChat { ChatId = chatId };
        var existingChat = dbContext.UserChats.FirstOrDefault(UserChat => UserChat.ChatId == chatId);
        if (existingChat == null)
        {
            dbContext.UserChats.Add(userChat);
            await dbContext.SaveChangesAsync();
        }
        else
        { }
    }
    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    try
    {
        string weatherMessage = await WeatherProgram.GetWeatherDataAsync(city, apiKey);
        string currencyMessage = await GetCurrency.GetCurrencyasync();
        if (messageText.StartsWith("/weather"))
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: weatherMessage, cancellationToken: cancellationToken);
        }
        else if (messageText.StartsWith("/currency"))
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: currencyMessage, cancellationToken: cancellationToken);
        }
        else if(messageText.StartsWith("asd"))
        {

        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Да-да? Лучше введи команду из доступного списка", cancellationToken: cancellationToken);
        }

    }
    catch (DbUpdateException ex)
    {
        // Обработка ошибки сохранения данных
        Console.WriteLine($"Ошибка сохранения данных: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.WriteLine($"Внутреннее исключение: {ex.InnerException.Message}");
            if (ex.InnerException.InnerException != null)
            {
                Console.WriteLine($"Внутреннее внутреннее исключение: {ex.InnerException.InnerException.Message}");
            }
        }

        // Отправляем сообщение об ошибке пользователю
        await botClient.SendTextMessageAsync(chatId: chatId, text: $"Произошла ошибка сохранения данных: {ex.Message}", cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        // Обработка других исключений
        Console.WriteLine($"Произошла ошибка: {ex.Message}");
        await botClient.SendTextMessageAsync(chatId: chatId, text: $"Произошла ошибка: {ex.Message}", cancellationToken: cancellationToken);
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


/*using (var dbContext = new BotDbContext())
{
    // Проверяем, существует ли запись для этого чата в базе данных
    var existingChat = dbContext.UserChats.FirstOrDefault(UserChat => UserChat.ChatId == chatId);
    if (existingChat == null)
    {
        var userChat = new UserChat { ChatId = chatId };
        dbContext.UserChats.Add(userChat);
        await dbContext.SaveChangesAsync();
    }
    else
    {
        return;
    }
}*/