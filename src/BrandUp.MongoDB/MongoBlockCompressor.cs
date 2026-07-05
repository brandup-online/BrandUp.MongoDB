namespace BrandUp.MongoDB
{
    /// <summary>
    /// WiredTiger block compressor applied to a collection at creation time via
    /// <c>storageEngine.wiredTiger.configString = "block_compressor=..."</c>. This is an
    /// immutable, create-only storage-engine option: it cannot be changed on an existing
    /// collection, so it is never reconciled.
    /// </summary>
    public enum MongoBlockCompressor
    {
        /// <summary>Do not set a block compressor; the server default (usually <c>snappy</c>) is used.</summary>
        Default = 0,

        /// <summary>No compression (<c>block_compressor=none</c>).</summary>
        None,

        /// <summary>Snappy compression (<c>block_compressor=snappy</c>).</summary>
        Snappy,

        /// <summary>Zlib compression (<c>block_compressor=zlib</c>).</summary>
        Zlib,

        /// <summary>Zstandard compression (<c>block_compressor=zstd</c>). Requires MongoDB 4.2+.</summary>
        Zstd
    }
}
