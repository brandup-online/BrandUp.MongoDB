using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace BrandUp.MongoDB
{
    public static class IMongoCollectionExtensions
    {
        /// <summary>
        /// Создание индексов в коллекции.
        /// </summary>
        /// <param name="collection">Коллекция документов.</param>
        /// <param name="indexes">Перечисление индексов, которые нужно создать.</param>
        /// <param name="recreateIfExists">Нужно ли пересоздавать индексы, которые уже существуют.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        public static async Task<IEnumerable<string>> ApplyIndexes<TDocument>(this IMongoIndexManager<TDocument> indexManager, IEnumerable<CreateIndexModel<TDocument>> indexes, bool recreateIfExists = true, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(indexManager);
            ArgumentNullException.ThrowIfNull(indexes);

            var indexesForCreation = new List<CreateIndexModel<TDocument>>();

            var currentIndexNames = (await ListNamesAsync(indexManager, cancellationToken)).Select(it => it.ToLower()).ToList();
            foreach (var index in indexes)
            {
                var indexName = index.Options.Name.ToLower();

                if (currentIndexNames.Contains(indexName))
                {
                    if (!recreateIfExists)
                        continue;

                    await indexManager.DropOneAsync(index.Options.Name, cancellationToken);
                }

                indexesForCreation.Add(index);
            }

            if (indexesForCreation.Count > 0)
                return await indexManager.CreateManyAsync(indexesForCreation, cancellationToken);
            else
                return [];
        }

        public static async Task<List<string>> ListNamesAsync<TDocument>(this IMongoIndexManager<TDocument> indexManager, CancellationToken cancellationToken = default)
        {
            return (await (await indexManager.ListAsync(new ListIndexesOptions(), cancellationToken)).ToListAsync(cancellationToken)).Select(it => it["name"].AsString).ToList();
        }

        public static async Task<bool> HasIndexAsync<TDocument>(this IMongoIndexManager<TDocument> indexManager, string name, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(name);

            var indexNames = await indexManager.ListNamesAsync(cancellationToken);
            foreach (var indexName in indexNames)
            {
                if (indexName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static async Task<bool> DropIfExistAsync<TDocument>(this IMongoIndexManager<TDocument> indexManager, string name, DropIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (!await HasIndexAsync(indexManager, name, cancellationToken))
                return false;

            await indexManager.DropOneAsync(name, options, cancellationToken: cancellationToken);

            return true;
        }
    }
}