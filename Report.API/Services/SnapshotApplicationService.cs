using Report.API.Entity;
using Report.API.Models.InputModels;
using Report.API.Models.ViewModels;
using Report.API.Repository;

namespace Report.API.Services;

public class SnapshotApplicationService(
    ReportRepository repository,
    ILogger<SnapshotApplicationService> logger)
{
    public async Task<List<InventoryBalanceResultViewModel>> GetInventoryBalanceAsync(
        CalculateSnaphotInputModel model,
        CancellationToken cancellationToken)
    {
        var snapshots = await GetOrCreateSnapshotsAsync(
            model.ProductIds,
            model.StockIds,
            model.InitalDate,
            cancellationToken);

        var delta = await repository.CalculateInventorySnapshotAsync(
            model.ProductIds,
            model.StockIds,
            model.InitalDate,
            model.FinalDate,
            cancellationToken);

        var deltaDict = delta.ToDictionary(
            x => $"{x.ProdutoID}_{x.DepositoID}",
            x => x.Saldo);

        var result = snapshots.Select(snapshot =>
        {
            var key = $"{snapshot.ProdutoId}_{snapshot.DepositoId}";

            var deltaValue = deltaDict.TryGetValue(key, out var d) ? d : 0;

            return new InventoryBalanceResultViewModel
            {
                ProdutoID = snapshot.ProdutoId,
                DepositoID = snapshot.DepositoId,
                Saldo = snapshot.Saldo + deltaValue
            };
        }).ToList();

        logger.LogInformation(
            "Calculated final inventory balance using snapshot + delta for {ProductCount} products and {StockCount} stocks.",
            model.ProductIds.Count,
            model.StockIds.Count);

        return result;
    }
    
    private async Task<List<InventorySnapshot>> GetOrCreateSnapshotsAsync(
        List<string> productIds,
        List<string> stockIds,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        var existingSnapshots = await repository.GetLatestSnapshotsAsync(
            productIds,
            stockIds,
            referenceDate,
            cancellationToken);

        var existingKeys = existingSnapshots
            .Select(x => $"{x.ProdutoId}_{x.DepositoId}")
            .ToHashSet();

        var allCombinations = productIds
            .SelectMany(p => stockIds, (p, s) => new { ProdutoId = p, DepositoId = s })
            .ToList();

        var missing = allCombinations
            .Where(x => !existingKeys.Contains($"{x.ProdutoId}_{x.DepositoId}"))
            .ToList();

        if (!missing.Any())
            return existingSnapshots;

        var closingDate = referenceDate.AddMilliseconds(-1);
        
        var calculated = await repository.CalculateInventorySnapshotAsync(
            missing.Select(x => x.ProdutoId).Distinct().ToList(),
            missing.Select(x => x.DepositoId).Distinct().ToList(),
            DateTime.MinValue,
            closingDate,
            cancellationToken);

        var snapshots = calculated.Select(x => new InventorySnapshot
        {
            ProdutoId = x.ProdutoID,
            DepositoId = x.DepositoID,
            Saldo = x.Saldo,
            DataFechamento = closingDate,
            Ano = closingDate.Year,
            Mes = closingDate.Month
        }).ToList();

        await repository.InsertManyInventorySnapshotAsync(snapshots, cancellationToken);

        logger.LogInformation(
            "Created {Count} new snapshots for reference date {ReferenceDate}.",
            snapshots.Count,
            referenceDate);

        return existingSnapshots
            .Concat(snapshots)
            .ToList();
    }
}