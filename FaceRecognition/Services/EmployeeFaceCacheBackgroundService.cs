namespace FaceRecognition.Services;

public class EmployeeFaceCacheBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private const int IndustryId = 1;

    public EmployeeFaceCacheBackgroundService(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        Console.WriteLine(
            "[FACE CACHE] Background service started.");


        // ========================================================
        // INITIAL LOAD
        // ========================================================

        try
        {
            using var scope =
                _scopeFactory.CreateScope();

            var cache =
                scope.ServiceProvider
                    .GetRequiredService<EmployeeFaceCacheService>();

            await cache.RefreshAsync(IndustryId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[FACE CACHE] Initial load failed: " +
                $"{ex.Message}");
        }


        // ========================================================
        // EVERY 10 MINUTES
        // ========================================================

        using var timer =
            new PeriodicTimer(
                TimeSpan.FromMinutes(10));


        while (await timer.WaitForNextTickAsync(
            stoppingToken))
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var cache =
                    scope.ServiceProvider
                        .GetRequiredService<EmployeeFaceCacheService>();

                await cache.RefreshAsync(IndustryId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FACE CACHE] Background refresh failed: " +
                    $"{ex.Message}");
            }
        }
    }
}