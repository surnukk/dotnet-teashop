using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace Reviews.Infrastructure.Persistence;

public class MongoDbContext
{
    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("MongoDb:ConnectionString is not configured");
        if (string.IsNullOrWhiteSpace(settings.DatabaseName))
            throw new InvalidOperationException("MongoDb:DatabaseName is not configured");

        var mongoSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        mongoSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        Client = new MongoClient(mongoSettings);
        Database = Client.GetDatabase(settings.DatabaseName);
    }

    public IMongoClient Client { get; }
    public IMongoDatabase Database { get; }

    public IMongoCollection<Review> Reviews => Database.GetCollection<Review>("reviews");
}