using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BrandUp.MongoDB.Testing.EphemeralMongo.Tests.Contract
{
    /// <summary>Document type shared by all collection contract scenarios.</summary>
    public class ContractDocument
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Counter { get; set; }
        public int Limit { get; set; }
        public long Total { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<ContractItem> Items { get; set; } = [];
        [BsonIgnoreIfNull]
        public string? Email { get; set; }
        [BsonIgnoreIfNull]
        public string? Type { get; set; }
        public DateTime LastSeen { get; set; }
        [BsonIgnoreIfNull]
        public ContractProfile? Profile { get; set; }
    }

    public class ContractProfile
    {
        public string City { get; set; } = null!;
    }

    public class ContractItem
    {
        public string Key { get; set; } = null!;
        public int Score { get; set; }
    }

    /// <summary>
    /// Document with an ObjectId key for upsert scenarios whose filter carries no _id:
    /// a real server generates an ObjectId for the inserted document, so a Guid-keyed
    /// class could not read it back.
    /// </summary>
    public class ContractSequenceDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public int Counter { get; set; }
    }
}
