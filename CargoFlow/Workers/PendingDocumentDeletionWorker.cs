using CargoFlow.Services;

namespace CargoFlow.Workers;

public class PendingDocumentDeletionWorker(
    ILogger<PendingDocumentDeletionWorker> logger,
    IServiceScopeFactory serviceScopeFactory
) : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_period);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var pendingDocumentDeletionService = scope.ServiceProvider
                    .GetRequiredService<IPendingDocumentDeletionService>();
                await pendingDocumentDeletionService.DeletePendingDocuments(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while deleting pending documents");
            }

            bool hasNext = await timer.WaitForNextTickAsync(stoppingToken);
            if (!hasNext)
            {
                break;
            }
        }
    }
}
