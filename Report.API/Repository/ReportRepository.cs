using MongoDB.Bson;
using MongoDB.Driver;
using Report.API.Entity;
using Report.API.Models.ViewModels;

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

    public async Task<PagedResultViewModel<StockIn>> GetStockInAsync(
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

        return new PagedResultViewModel<StockIn>
        {
            Items = items,
            Count = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultViewModel<StockOut>> GetStockOutAsync(
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

        return new PagedResultViewModel<StockOut>
        {
            Items = items,
            Count = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<InventoryBalanceResultViewModel>> CalulateInventorySnapshotAsync(
        List<string> productIds,
        List<string> stockIds,
        DateTime initalDate,
        DateTime finalDate,
        CancellationToken cancellationToken)
    {
        var collectionStockIn = _database.GetCollection<BsonDocument>(nameof(StockIn));

        var defaultFilter = new BsonDocument
        {
            { "ProdutoID", new BsonDocument("$in", new BsonArray(productIds)) },
            { "DepositoID", new BsonDocument("$in", new BsonArray(stockIds)) },
            { "Data", new BsonDocument {
                { "$gte", initalDate },
                { "$lte", finalDate }
            }}
        };

        var pipeline = new List<BsonDocument>
        { 
            // 1. StockIn
            new BsonDocument("$match", defaultFilter),

            new BsonDocument("$project", new BsonDocument
            {
                { "ProdutoID", 1 },
                { "DepositoID", 1 },
                { "Quantidade", 1 },
                { "Tipo", new BsonDocument("$literal", "E") },
            }),

            // 2. Union With Stock Out
            new BsonDocument("$unionWith", new BsonDocument
            {
                { "coll", "StockOut" },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", defaultFilter),
                        new BsonDocument("$project", new BsonDocument
                        {
                            { "ProdutoID", 1 },
                            { "DepositoID", 1 },
                            { "Quantidade", 1 },
                            { "Tipo", new BsonDocument("$literal", "S") },
                        })
                    }
                }
            }),

            // 3. Group (stock)
            new BsonDocument("$group", new BsonDocument
            {
                {
                    "_id", new BsonDocument
                    {
                        { "ProdutoID", "$ProdutoID" },
                        { "DepositoID", "$DepositoID" }
                    }
                },
                {
                    "Saldo", new BsonDocument("$sum",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { "$Tipo", "E" }),
                            "$Quantidade",
                            new BsonDocument("$multiply", new BsonArray { "$Quantidade", -1 })
                        })
                    )
                }
            }),

            // 4. Flatten
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "ProdutoID", "$_id.ProdutoID" },
                { "DepositoID", "$_id.DepositoID" },
                { "Saldo", 1 }
            })
        };

        var result = await collectionStockIn
            .Aggregate<InventoryBalanceResultViewModel>(pipeline, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);

        return result;
    }
}