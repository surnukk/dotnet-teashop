using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// BSON-мапінг доменних класів без атрибутів у Domain.
/// Реєструється один раз під час старту, до першого звернення до БД.
/// </summary>
public static class MongoMappings
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered) return;

            ConventionRegistry.Register("camelCase",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new IgnoreExtraElementsConvention(true),
                    new IgnoreIfNullConvention(true) 
                },
                _ => true);

            BsonSerializer.RegisterSerializer(new RatingSerializer());

            BsonClassMap.RegisterClassMap<Review>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_comments").SetElementName("comments");   // приватне поле
            });

            BsonClassMap.RegisterClassMap<Comment>(cm => cm.AutoMap());

            BsonClassMap.RegisterClassMap<Author>(cm =>
            {
                cm.MapProperty(a => a.UserId).SetElementName("userId");
                cm.MapProperty(a => a.DisplayName).SetElementName("displayName");
                cm.MapCreator(a => new Author(a.UserId, a.DisplayName));   // незмінний VO
                cm.SetIgnoreExtraElements(true);
            });

            _registered = true;
        }
    }
}