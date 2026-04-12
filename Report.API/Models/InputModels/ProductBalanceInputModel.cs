namespace Report.API.Models.InputModels;

public record ProductBalanceInputModel(
    List<string> ProductIds,
    List<string> StockIds,
    DateTime FinalDate);
