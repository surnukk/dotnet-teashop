using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence;

public sealed class RatingSerializer : SerializerBase<Rating>
{
    public override Rating Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var value = reader.CurrentBsonType switch
        {
            BsonType.Int32 => reader.ReadInt32(),
            BsonType.Int64 => (int)reader.ReadInt64(),
            BsonType.Double => (int)reader.ReadDouble(),
            _ => throw new FormatException($"Cannot deserialize Rating from {reader.CurrentBsonType}")
        };
        return new Rating(value);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Rating value)
        => context.Writer.WriteInt32(value.Value);
}