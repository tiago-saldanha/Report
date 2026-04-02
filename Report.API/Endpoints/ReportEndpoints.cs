using Microsoft.AspNetCore.Mvc;
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
    }

    private static async Task<IResult> ListCollections(
        [FromServices] ReportApplicationService service,
        CancellationToken cancellationToken)
    {
        var collections = await service.ListCollectionsAsync(cancellationToken);

        return Results.Ok(new { Collections = collections });
    }

    private static async Task<IResult> GetStockIn(
        [FromQuery] string produtoId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] ReportApplicationService service,
        CancellationToken cancellationToken)
    {
        var entradas = await service.GetStockInAsync(produtoId, page, pageSize, cancellationToken);

        if (entradas is null)
            return Results.NotFound();

        return Results.Ok(entradas);
    }

    private static async Task<IResult> GetStockOut(
        [FromQuery] string produtoId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] ReportApplicationService service,
        CancellationToken cancellationToken)
    {
        var saidas = await service.GetStockOutAsync(produtoId, page, pageSize, cancellationToken);

        if (saidas is null)
            return Results.NotFound();

        return Results.Ok(saidas);
    }
}