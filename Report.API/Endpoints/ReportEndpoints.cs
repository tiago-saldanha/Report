using Microsoft.AspNetCore.Mvc;
using Report.API.Models.InputModels;
using Report.API.Services;

namespace Report.API.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/report")
                       .WithTags("Report");

        group.MapGet("/list-collections", ListCollections)
             .WithName("ListCollections");

        group.MapGet("/stock-in", GetStockIn)
             .WithName("GetStockIn");

        group.MapGet("/stock-out", GetStockOut)
             .WithName("GetStockOut");

        group.MapPost("/inventory-balance", GetInventoryBalance)
            .WithName("GetInventoryBalance");
    }

    private static async Task<IResult> ListCollections(
        [FromServices] ReportApplicationService service,
        CancellationToken cancellationToken)
    {
        var collections = await service.ListCollectionsAsync(cancellationToken);
        return Results.Ok(new { Collections = collections });
    }

    private static async Task<IResult> GetStockIn(
        [FromQuery] string productId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] ReportApplicationService service,
        CancellationToken cancellationToken)
    {
        var stockIn = await service.GetStockInAsync(productId, page, pageSize, cancellationToken);

        if (stockIn is null)
            return Results.NotFound();

        return Results.Ok(stockIn);
    }

    private static async Task<IResult> GetStockOut(
        [FromQuery] string productId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] ReportApplicationService service,
        CancellationToken cancellationToken)
    {
        var stockOut = await service.GetStockOutAsync(productId, page, pageSize, cancellationToken);

        if (stockOut is null)
            return Results.NotFound();

        return Results.Ok(stockOut);
    }

    private static async Task<IResult> GetInventoryBalance(
        [FromBody] ProductBalanceInputModel model,
        [FromServices] SnapshotApplicationService service,
        CancellationToken cancellationToken)
    {
        var results = await service.GetInventoryBalanceAsync(model, cancellationToken);
        return Results.Ok(results);
    }
}