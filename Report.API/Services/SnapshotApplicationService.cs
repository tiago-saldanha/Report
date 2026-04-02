using Report.API.Models.InputModels;
using Report.API.Models.ViewModels;
using Report.API.Repository;

namespace Report.API.Services;

public class SnapshotApplicationService(ReportRepository repository, ILogger<SnapshotApplicationService> logger)
{
    public async Task<List<InventoryBalanceResultViewModel>> CalulateInventorySnapshotAsync(
        CalculateSnaphotInputModel model,
        CancellationToken cancellationToken)
    {
        var result = await repository.CalulateInventorySnapshotAsync(
            model.ProductIds, 
            model.StockIds, 
            model.InitalDate, 
            model.FinalDate, 
            cancellationToken);

        logger.LogInformation(
            "Calculated inventory snapshot for {ProductCount} products and {StockCount} stocks between {InitialDate} and {FinalDate}.",
            model.ProductIds.Count, 
            model.StockIds.Count, 
            model.InitalDate, 
            model.FinalDate);

        return result;
    }
}
