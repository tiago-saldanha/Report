using Report.API.Abstract;

namespace Report.API.Entity;

public class InventorySnapshot : EntityLastUpdate
{
    public string ProdutoId { get; set; }
    public string DepositoId { get; set; }
    public int Ano { get; set; }
    public int Mes { get; set; }
    public double Saldo { get; set; }
    public DateOnly DataFechamento { get; set; }
}
