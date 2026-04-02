using Report.API.Abstract;

namespace Report.API.Entity;

public class StockIn : EntityLastUpdate
{
    public int CodigoProduto { get; set; }
    public string CodigoProdutoNFE { get; set; }
    public string ProdutoID { get; set; }
    public string DepositoID { get; set; }
    public string Produto { get; set; }
    public string Deposito { get; set; }
    public string Movimentacao { get; set; }
    public DateTime Data { get; set; }
    public double Quantidade { get; set; }
    public string Observacoes { get; set; }
}
