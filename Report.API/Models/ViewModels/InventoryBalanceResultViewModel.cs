namespace Report.API.Models.ViewModels;

public class InventoryBalanceResultViewModel
{
    public string ProdutoID { get; set; }
    public string DepositoID { get; set; }
    public decimal Saldo { get; set; }
}
