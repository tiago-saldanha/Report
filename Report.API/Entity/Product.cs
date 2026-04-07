using MongoDB.Bson.Serialization.Attributes;
using Report.API.Abstract;

namespace Report.API.Entity;

[BsonIgnoreExtraElements]
public class Product : EntityLastUpdate
{
    public string Nome { get; set; }
    public string CodigoNFe { get; set; }
    public string CategoriaID { get; set; }
}
