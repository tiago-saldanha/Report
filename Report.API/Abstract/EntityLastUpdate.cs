using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Report.API.Abstract;

[BsonIgnoreExtraElements]
public abstract class EntityLastUpdate
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public DateTime LastUpdate { get; set; }
}
