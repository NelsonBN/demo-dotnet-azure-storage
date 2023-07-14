using System.Text.Json;
using Azure.Storage.Queues;

namespace Demo.Api;

public class Worker : BackgroundService
{
    private static readonly TimeSpan _delay = TimeSpan.FromSeconds(5);
    private readonly ILogger<Worker> _logger;
    private readonly QueueClient _queue;

    public Worker(
        ILogger<Worker> logger,
        QueueClient queue)
    {
        _logger = logger;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Keep worker running
            while(!stoppingToken.IsCancellationRequested)
            {
                var messages = await _queue.ReceiveMessagesAsync(maxMessages: 10, cancellationToken: stoppingToken);
                if(messages.Value.Length == 0)
                {
                    _logger.LogInformation("[WORKER] running at: {time}", DateTimeOffset.UtcNow);
                    await Task.Delay(_delay, stoppingToken);
                    continue;
                }

                foreach(var item in messages.Value)
                {
                    var file = JsonSerializer.Deserialize<Image>(item.MessageText);

                    _logger.LogInformation("[WORKER] File sent: {file}", file);

                    await _queue.DeleteMessageAsync(item.MessageId, item.PopReceipt, stoppingToken);
                }
            }
        }
        catch(Exception exception)
        {
            _logger.LogError(
                exception,
                "[WORKER] Error while processing logs");
        }
    }
}
