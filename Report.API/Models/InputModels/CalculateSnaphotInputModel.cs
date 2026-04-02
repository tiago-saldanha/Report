namespace Report.API.Models.InputModels
{
    public record CalculateSnaphotInputModel(
        List<string> ProductIds,
        List<string> StockIds,
        DateTime InitalDate,
        DateTime FinalDate);
}
