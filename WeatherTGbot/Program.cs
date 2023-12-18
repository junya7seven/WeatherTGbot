using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
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


// Остальной код вашего приложения Telegram Bot остается таким же, как ранее.

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message || message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    try
    {
        using (var dbContext = new BotDbContext())
        {
            var existingChat = dbContext.UserChats.FirstOrDefault(uc => uc.ChatId == chatId);
            if (existingChat == null && messageText.StartsWith("/getID"))
            {
                var userChat = new UserChat { ChatId = chatId };
                dbContext.UserChats.Add(userChat);
                await dbContext.SaveChangesAsync();

                await botClient.SendTextMessageAsync(chatId: chatId, text: "Ваш ID был сохранён.", cancellationToken: cancellationToken);
            }
            else if (existingChat != null && messageText.StartsWith("/getID"))
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Ваш ID уже был сохранён.", cancellationToken: cancellationToken);
            }

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
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Да-да? Лучше введи команду из доступного списка", cancellationToken: cancellationToken);
            }
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
public class BotDbContext : DbContext
{
    public DbSet<UserChat> UserChats { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
                optionsBuilder.UseSqlite("Data Source=mydatabase.db"); // Путь к файлу базы данных SQLite

    }
}
public class UserChat
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public long ChatId { get; set; }
}