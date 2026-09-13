using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenFridaysMusic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;
using Microsoft.Extensions.Logging;

ServiceCollection services = new();
services.AddSingleton<IMusicProvider, YoutubeParser>();
services.AddSingleton<YoutubeClient>();
services.AddLogging(builder => builder.AddConsole());
using var serviceProvider = services.BuildServiceProvider();
IMusicProvider musicprovider = serviceProvider.GetRequiredService<IMusicProvider>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

var config = new ConfigurationBuilder()

    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

string? botToken = config["BotSettings:Token"];

if (string.IsNullOrEmpty(botToken))
{
    throw new Exception("token not found");
}


using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient((botToken), cancellationToken: cts.Token);
var me = await bot.GetMe();
bot.OnMessage += OnMessage;
bot.OnUpdate += OnClickUpdate;
Console.WriteLine($"@{me.Username} is running... Press enter to end");
await Task.Delay(-1, cts.Token);
cts.Cancel();

async Task OnMessage(Message msg, UpdateType updateType) 
{
    if (string.IsNullOrWhiteSpace(msg.Text)) return;
    logger.LogInformation("Received message from {UserId}: {Text}", msg.From?.Id, msg.Text);
    var tracks = await musicprovider.SearchAsync(msg.Text, cts.Token);
    if(tracks.Count == 0) { await bot.SendMessage(msg.Chat, "Нет результатов..."); return; }
    var keyboardButtons = new List<List<InlineKeyboardButton>>();
    foreach (var track in tracks.Take(10))
    {
        var button = InlineKeyboardButton.WithCallbackData($"{track.Title} - {track.Author} ({track.Duration?.ToString(@"mm\:ss")})", $"download:{track.Id}");
        keyboardButtons.Add([button]);
    }

    await bot.SendMessage(
        chatId: msg.Chat,
        text:  $"Список песен по запросу: {msg.Text}",
        replyMarkup: new InlineKeyboardMarkup(keyboardButtons)
        );
}
async Task OnClickUpdate(Update upd)
{
    if (upd is { CallbackQuery: { } query })
    {
        if (query.Data == null || !query.Data.StartsWith("download:")) return;
        await bot.AnswerCallbackQuery(query.Id, cancellationToken: cts.Token);
        var mes = await bot.SendMessage(query.From, "(｡•̀ᴗ-)✧ 🎵 🎵 🎵\nНачинаю скачивание, подождите...");
        int mesId = mes.Id;
        string videoId = query.Data.Replace("download:", "");
        string? filePath = null;
        _ = Task.Run(async () =>
        {
            try
            {
                filePath = await musicprovider.DownloadAsync(videoId, cts.Token);
            }
            catch (Exception e) { logger.LogError(e, "Error occurred while downloading video {VideoId}", videoId); }
            await bot.DeleteMessage(query.From, mesId);
            if (filePath == null)
            {
                await bot.SendMessage(query.From, "Ошибка при скачивании файла");
                return;
            }
            try
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    var goMes = await bot.SendMessage(query.From, "(ﾉ◕ヮ◕)ﾉ*:･ﾟ✧ 🎵 🎵 🎵\nОтправляю аудио...");
                    int goMesId = goMes.Id;
                    InputFile audioFile = InputFile.FromStream(stream, Path.GetFileName(filePath));
                    await bot.SendAudio(chatId: query.From, audio: audioFile);
                    await bot.DeleteMessage(query.From, goMesId);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occurred while sending audio for video {VideoId}", videoId);
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        logger.LogInformation("Deleted file {FilePath}", filePath);
                    }
                    catch (Exception e) { logger.LogError(e, "Error occurred while deleting file {FilePath}", filePath); }
                }
            }
        });
    }
}
