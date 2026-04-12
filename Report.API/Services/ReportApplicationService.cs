using Report.API.Entity;
using Report.API.Models.InputModels;
using Report.API.Models.ViewModels;
using Report.API.Repository;

namespace Report.API.Services;

public class ReportApplicationService(ReportRepository repository, ILogger<ReportApplicationService> logger)
{
    public async Task<IEnumerable<string>> ListCollectionsAsync(
        CancellationToken cancellationToken)
        => await repository.ListCollectionsAsync(cancellationToken);

    public async Task<PagedResultViewModel<StockIn>> GetStockInAsync(
        string productId, 
        int page, 
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pagedResult = await repository.GetStockInAsync(
            productId, 
            page, 
            pageSize, 
            cancellationToken);

        logger.LogInformation(
            "Retrieved {Count} entries for product {ProdutoId} on page {Page} with page size {PageSize}.",
            pagedResult.Count, 
            productId, 
            page, 
            pageSize);

        return pagedResult;
    }

    public async Task<PagedResultViewModel<StockOut>> GetStockOutAsync(
        string productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pagedResult = await repository.GetStockOutAsync(
            productId,
            page,
            pageSize,
            cancellationToken);

        logger.LogInformation(
            "Retrieved {Count} entries for product {ProdutoId} on page {Page} with page size {PageSize}.",
            pagedResult.Count,
            productId,
            page,
            pageSize);

        return pagedResult;
    }
}
