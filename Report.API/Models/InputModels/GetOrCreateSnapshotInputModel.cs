namespace Report.API.Models.InputModels
{
    public record GetOrCreateSnapshotInputModel(
        List<string> ProductIds,
        List<string> StockIds,
        DateTime ReferenceDate);
}
