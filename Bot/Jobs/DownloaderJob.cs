using System.Net;
using Bot.Constants;
using Microsoft.Extensions.Options;
using Telegram.Bot.Exceptions;
using File = System.IO.File;

namespace Bot.Jobs;

[DisallowConcurrentExecution]
public class DownloaderJob : IJob
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<DownloaderJob> _logger;
    private readonly ServicesSettings _servicesSettings;
    private readonly HttpClient _client;

    public DownloaderJob(ITelegramBotClient bot, ILogger<DownloaderJob> logger,
        IOptions<ServicesSettings> servicesSettings, IAmazonSQS sqsClient, HttpClient client)
    {
        _bot = bot;
        _logger = logger;
        _sqsClient = sqsClient;
        _client = client;
        _servicesSettings = servicesSettings.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var response = await _sqsClient.ReceiveMessageAsync(_servicesSettings.DownloaderQueueUrl);
        var queueMessage = response.Messages.FirstOrDefault();

        if (queueMessage == null)
        {
            return;
        }

        var (receivedMessage, sentMessage, link, downloaderMessageType) = JsonSerializer.Deserialize<DownloaderMessage>(queueMessage.Body)!;

        try
        {
            if (sentMessage.Date < DateTime.UtcNow.AddDays(-2))
            {
                sentMessage = await _bot.SendTextMessageAsync(new(receivedMessage.Chat.Id),
                    "Downloading file 🚀",
                    replyToMessageId: receivedMessage.MessageId,
                    disableNotification: true);
            }
            else
            {
                await _bot.EditMessageTextAsync(new(sentMessage.Chat.Id),
                    sentMessage.MessageId,
                    "Downloading file 🚀");
            }

            var inputFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.webm");

            var handleMessageTask = downloaderMessageType switch
            {
                DownloaderMessageType.Link => HandleLinkAsync(receivedMessage, sentMessage, link, inputFilePath),
                DownloaderMessageType.Video => HandleFileBaseAsync(receivedMessage, sentMessage, inputFilePath, receivedMessage.Video.FileId),
                DownloaderMessageType.Document => HandleFileBaseAsync(receivedMessage, sentMessage, inputFilePath, receivedMessage.Document.FileId),
            };

            await handleMessageTask;

            await _sqsClient.DeleteMessageAsync(_servicesSettings.DownloaderQueueUrl, queueMessage.ReceiptHandle);
        }
        catch (ApiRequestException telegramException)
        {
            _logger.LogError(telegramException, "Telegram error during Uploader execution:");
            await _sqsClient.DeleteMessageAsync(_servicesSettings.DownloaderQueueUrl, queueMessage.ReceiptHandle);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during Downloader execution:");
        }
    }

    private async Task HandleLinkAsync(Message receivedMessage, Message sentMessage, string linkOrFileName, string inputFilePath)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, linkOrFileName);

        using var response = await _client.SendAsync(request);

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => $"{linkOrFileName}\nI am not authorized to download video from this source 🚫",
            HttpStatusCode.Forbidden => $"{linkOrFileName}\nMy access to this video is forbidden 🚫",
            HttpStatusCode.NotFound => $"{linkOrFileName}\nVideo not found ⚠️",
            HttpStatusCode.InternalServerError => $"{linkOrFileName}\nServer error 🛑",
            _ => null
        };

        if (message != null)
        {
            await _bot.EditMessageTextAsync(new(sentMessage.Chat.Id),
                sentMessage.MessageId,
                message);

            return;
        }

        await using var fileStream = File.Create(inputFilePath);

        await response.Content.CopyToAsync(fileStream);

        await SendMessageAsync(receivedMessage, sentMessage, inputFilePath);
    }

    private async Task HandleFileBaseAsync(Message receivedMessage, Message sentMessage, string inputFileName,
        string fileId)
    {
        await using (var fileStream = File.Create(inputFileName))
        {
            await _bot.GetInfoAndDownloadFileAsync(fileId, fileStream);
        }

        await SendMessageAsync(receivedMessage, sentMessage, inputFileName);
    }

    private async Task SendMessageAsync(Message receivedMessage, Message sentMessage, string inputFilePath)
    {
        var converterMessage = new ConverterMessage(receivedMessage, sentMessage, inputFilePath);

        await _sqsClient.SendMessageAsync(_servicesSettings.ConverterQueueUrl,
            JsonSerializer.Serialize(converterMessage, JsonSerializerConstants.SerializerOptions));

        await _bot.EditMessageTextAsync(new(sentMessage.Chat.Id),
            sentMessage.MessageId,
            "Your file is waiting to be converted 🕒");
    }
}