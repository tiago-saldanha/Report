
using Report.API.Services;

namespace Report.API.Workers;

public class SnapshotWorkerService(
    SnapshotApplicationService service,
    ILogger<SnapshotWorkerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SnapshotWorkerService started");

        await service.GenerateSnapshotsAsync(stoppingToken);

        logger.LogInformation("SnapshotWorkerService stoped");
    }
}
