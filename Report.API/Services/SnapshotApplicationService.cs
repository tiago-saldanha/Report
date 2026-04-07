using MongoDB.Driver;
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
            "Calculated final inventory balance for {ProductCount} products. Snapshots used: {SnapshotCount}",
            model.ProductIds.Count,
            snapshots.Count);

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
            .SelectMany(p => stockIds, (p, s) => new { ProdutoId = p, DepositoId = s });

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

        var newSnapshots = calculated.Select(x => new InventorySnapshot
        {
            ProdutoId = x.ProdutoID,
            DepositoId = x.DepositoID,
            Saldo = x.Saldo,
            DataFechamento = closingDate,
            Ano = closingDate.Year,
            Mes = closingDate.Month
        }).ToList();

        if (newSnapshots.Any())
        {
            await repository.UpsertInventorySnapshotsAsync(newSnapshots, cancellationToken);
            logger.LogInformation("Created {Count} new missing snapshots.", newSnapshots.Count);
        }

        return existingSnapshots.Concat(newSnapshots).ToList();
    }

    public async Task GenerateSnapshotsAsync(
        CancellationToken cancellationToken)
    {
        using var cursor = repository.GetProductsAsync(cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var product in cursor.Current)
            {
                try
                {
                    var stockInTask = GetStockInsAsync(product, cancellationToken);
                    var stockOutTask = GetStockOutsAsync(product, cancellationToken);

                    await Task.WhenAll(stockInTask, stockOutTask);

                    var stockIns = await stockInTask;
                    var stockOuts = await stockOutTask;

                    var depositIds = stockIns.Select(x => x.DepositoID)
                        .Union(stockOuts.Select(x => x.DepositoID))
                        .Distinct();

                    await Parallel.ForEachAsync(depositIds, async (depId, CancellationToken) =>
                    {
                        var filteredIns = stockIns.Where(x => x.DepositoID == depId).ToList();
                        var filteredOuts = stockOuts.Where(x => x.DepositoID == depId).ToList();

                        var snapshots = CalculateSnapshots(product.Id, depId, filteredIns, filteredOuts);

                        if (snapshots.Any())
                        {
                            await repository.UpsertInventorySnapshotsAsync(snapshots, cancellationToken);
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing product {ProductId}", product.Id);
                }
            }
        }
    }

    private List<InventorySnapshot> CalculateSnapshots(
        string produtoId,
        string depositoId,
        List<StockIn> allStockIns,
        List<StockOut> allStockOuts)
    {
        var allMovements = allStockIns
            .Select(i => new { i.Data, Quantidade = (double)i.Quantidade })
            .Concat(allStockOuts.Select(o => new { o.Data, Quantidade = (double)-o.Quantidade }));

        var monthlyGroups = allMovements
            .GroupBy(x => new { x.Data.Year, x.Data.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                VariacaoMensal = g.Sum(x => x.Quantidade),
                DataFechamento = new DateTime(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month), 23, 59, 59)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        var snapshots = new List<InventorySnapshot>();
        double saldoAcumulado = 0;

        foreach (var group in monthlyGroups)
        {
            saldoAcumulado += group.VariacaoMensal;

            snapshots.Add(new InventorySnapshot
            {
                ProdutoId = produtoId,
                DepositoId = depositoId,
                Ano = group.Year,
                Mes = group.Month,
                Saldo = saldoAcumulado,
                DataFechamento = group.DataFechamento,
            });
        }

        return snapshots;
    }

    private async Task<List<StockIn>> GetStockInsAsync(
        Product product, 
        CancellationToken cancellationToken)
    {
        var page = 1;
        const int pageSize = 1000;
        var allItems = new List<StockIn>();

        PagedResultViewModel<StockIn> result;
        do
        {
            result = await repository.GetStockInAsync(product.Id, page, pageSize, cancellationToken);
            if (result.Items == null || !result.Items.Any()) break;

            allItems.AddRange(result.Items);
            page++;
        } while (allItems.Count < result.Count);

        return allItems;
    }

    private async Task<List<StockOut>> GetStockOutsAsync(
        Product product, 
        CancellationToken cancellationToken)
    {
        var page = 1;
        const int pageSize = 1000;
        var allItems = new List<StockOut>();

        PagedResultViewModel<StockOut> result;
        do
        {
            result = await repository.GetStockOutAsync(product.Id, page, pageSize, cancellationToken);
            if (result.Items == null || !result.Items.Any()) break;

            allItems.AddRange(result.Items);
            page++;
        } while (allItems.Count < result.Count);

        return allItems;
    }
}