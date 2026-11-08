namespace CargoFlow.Workers;

public class OfferExpirationWorker(ILogger<OfferExpirationWorker> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromDays(1);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var offerExpirationService = scope.ServiceProvider.GetRequiredService<IOfferExpirationService>();
                await offerExpirationService.CheckAndMarkExpirationOfferAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while executing offers expiration background task");
            }

            var hasNext = await timer.WaitForNextTickAsync(stoppingToken);
            if (!hasNext)
            {
                break;
            }
        }
    }

}