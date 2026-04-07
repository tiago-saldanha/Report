using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Bson;
using MongoDB.Driver;
using Report.API.Entity;
using Report.API.Models.ViewModels;

namespace Report.API.Repository;

public class ReportRepository
{
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(ILogger<ReportRepository> logger)
    {
        _client = new MongoClient("mongodb://localhost:27017");
        _database = _client.GetDatabase("database");
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
        string productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<StockIn>(nameof(StockIn));
        var filter = Builders<StockIn>.Filter.Eq(x => x.ProdutoID, productId);

        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await collection
            .Find(filter)
            .SortByDescending(x => x.Data)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} StockIn items for produtoId {ProdutoId} (page {Page})",
            items.Count, productId, page);

        return new PagedResultViewModel<StockIn>
        {
            Items = items,
            Count = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultViewModel<StockOut>> GetStockOutAsync(
        string productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<StockOut>(nameof(StockOut));
        var filter = Builders<StockOut>.Filter.Eq(x => x.ProdutoID, productId);

        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await collection
            .Find(filter)
            .SortByDescending(x => x.Data)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} StockOut items for produtoId {ProdutoId} (page {Page})",
            items.Count, productId, page);

        return new PagedResultViewModel<StockOut>
        {
            Items = items,
            Count = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<InventoryBalanceResultViewModel>> CalculateInventorySnapshotAsync(
        List<string> productIds,
        List<string> stockIds,
        DateTime initialDate,
        DateTime finalDate,
        CancellationToken cancellationToken)
    {
        var collectionStockIn = _database.GetCollection<BsonDocument>(nameof(StockIn));

        var filter = new BsonDocument
        {
            { "ProdutoID", new BsonDocument("$in", new BsonArray(productIds)) },
            { "DepositoID", new BsonDocument("$in", new BsonArray(stockIds)) },
            { "Data", new BsonDocument {
                { "$gte", initialDate },
                { "$lte", finalDate }
            }}
        };

        var pipeline = new List<BsonDocument>
        {
            new BsonDocument("$match", filter),
            new BsonDocument("$project", new BsonDocument
            {
                { "ProdutoID", 1 },
                { "DepositoID", 1 },
                { "Quantidade", 1 },
                { "Tipo", new BsonDocument("$literal", "E") },
            }),
            new BsonDocument("$unionWith", new BsonDocument
            {
                { "coll", nameof(StockOut) },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", filter),
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
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "ProdutoID", "$_id.ProdutoID" },
                { "DepositoID", "$_id.DepositoID" },
                { "Saldo", 1 }
            })
        };

        return await collectionStockIn
            .Aggregate<InventoryBalanceResultViewModel>(pipeline, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<InventorySnapshot>> GetLatestSnapshotsAsync(
        List<string> productIds,
        List<string> stockIds,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<InventorySnapshot>(nameof(InventorySnapshot));

        var filter = Builders<InventorySnapshot>.Filter.And(
            Builders<InventorySnapshot>.Filter.In(x => x.ProdutoId, productIds),
            Builders<InventorySnapshot>.Filter.In(x => x.DepositoId, stockIds),
            Builders<InventorySnapshot>.Filter.Lte(x => x.DataFechamento, referenceDate)
        );

        var snapshots = await collection
            .Find(filter)
            .SortByDescending(x => x.DataFechamento)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(x => new { x.ProdutoId, x.DepositoId })
            .Select(g => g.First())
            .ToList();
    }

    public IAsyncCursor<Product> GetProductsAsync(
        CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<Product>(nameof(Product));
        return collection.FindSync<Product>(FilterDefinition<Product>.Empty, cancellationToken: cancellationToken);
    }

    public async Task UpsertInventorySnapshotsAsync(
        IEnumerable<InventorySnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        try
        {
            if (snapshots == null || !snapshots.Any())
                return;

            var collection = _database.GetCollection<InventorySnapshot>(nameof(InventorySnapshot));

            var models = snapshots.Select(snapshot =>
            {
                var filter = Builders<InventorySnapshot>.Filter.And(
                    Builders<InventorySnapshot>.Filter.Eq(x => x.ProdutoId, snapshot.ProdutoId),
                    Builders<InventorySnapshot>.Filter.Eq(x => x.DepositoId, snapshot.DepositoId),
                    Builders<InventorySnapshot>.Filter.Eq(x => x.DataFechamento, snapshot.DataFechamento)
                );

                var update = Builders<InventorySnapshot>.Update
                    .Set(x => x.Saldo, snapshot.Saldo)
                    .Set(x => x.Ano, snapshot.Ano)
                    .Set(x => x.Mes, snapshot.Mes)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .SetOnInsert(x => x.ProdutoId, snapshot.ProdutoId)
                    .SetOnInsert(x => x.DepositoId, snapshot.DepositoId)
                    .SetOnInsert(x => x.DataFechamento, snapshot.DataFechamento);

                return new UpdateOneModel<InventorySnapshot>(filter, update)
                {
                    IsUpsert = true
                };
            }).ToList();

            await collection.BulkWriteAsync(models, new BulkWriteOptions
            {
                IsOrdered = false
            }, cancellationToken);

            _logger.LogInformation(
                "Insert InventorySnapshot: {count}",
                models.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error to upsert Snapshots: {message}", ex.Message);
        }
    }
}