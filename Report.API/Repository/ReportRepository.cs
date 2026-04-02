using MongoDB.Driver;
using Report.API.Entity;
using Report.API.Models;

namespace Report.API.Repository;

public class ReportRepository
{
    private readonly string _uri = "mongodb://localhost:27017";
    private readonly string _databaseName = "database";
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(ILogger<ReportRepository> logger)
    {
        _client = new MongoClient(_uri);
        _database = _client.GetDatabase(_databaseName, new MongoDatabaseSettings());
        _logger = logger;
    }

    public async Task<IEnumerable<string>> ListCollectionsAsync(
        CancellationToken cancellationToken)
    {
        var collections = await _database.ListCollectionsAsync(null, cancellationToken);

        return collections
            .ToEnumerable(cancellationToken: cancellationToken)
            .Select(item => item["name"].AsString);
    }

    public async Task<PagedResult<StockIn>> GetStockInAsync(
        string produtoId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<StockIn>(nameof(StockIn));

        var filter = Builders<StockIn>.Filter.Eq(x => x.ProdutoID, produtoId);

        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await collection
            .Find(filter)
            .SortByDescending(x => x.Data)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} items for produtoId {ProdutoId} on page {Page} with page size {PageSize}",
            items.Count, 
            produtoId, 
            page, 
            pageSize);

        return new PagedResult<StockIn>
        {
            Items = items,
            Count = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<StockOut>> GetStockOutAsync(
        string produtoId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<StockOut>(nameof(StockOut));

        var filter = Builders<StockOut>.Filter.Eq(x => x.ProdutoID, produtoId);

        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await collection
            .Find(filter)
            .SortByDescending(x => x.Data)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} items for produtoId {ProdutoId} on page {Page} with page size {PageSize}",
            items.Count, 
            produtoId, 
            page, 
            pageSize);

        return new PagedResult<StockOut>
        {
            Items = items,
            Count = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task CreateInventorySnapshotAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}