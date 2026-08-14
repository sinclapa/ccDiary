// <copyright file="TableJson.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using Azure;
    using Azure.Data.Tables;

    /// <summary>
    /// Shared serialisation and query helpers for the table-backed services.
    /// </summary>
    /// <remarks>
    /// Every service stores its entity as a single serialised <c>Json</c> column plus a
    /// handful of broken-out columns that need to be queryable. Centralising that here
    /// keeps one definition of the on-disk format; five services each rolling their own
    /// would drift and would trip duplication analysis.
    /// </remarks>
    public static class TableJson
    {
        /// <summary>The column holding the serialised entity.</summary>
        public const string JsonColumn = "Json";

        /// <summary>
        /// The column recording which revision of the storage shape wrote a row.
        /// </summary>
        /// <remarks>
        /// There is no migration step in a schema-less store, so this is the only way to
        /// tell, after the fact, which rows predate a change in shape.
        /// </remarks>
        public const string SchemaVersionColumn = "SchemaVersion";

        /// <summary>The current storage shape revision, written on every upsert.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Gets the serializer options used for the <c>Json</c> column.
        /// </summary>
        /// <remarks>
        /// Enums are written kebab-case to match the HTTP contract, so a stored value
        /// reads the same as the wire value. Storing them as strings rather than
        /// ordinals also means reordering an enum cannot silently reinterpret old rows.
        /// </remarks>
        public static JsonSerializerOptions Options { get; } = CreateOptions();

        /// <summary>Serialises an entity for the <c>Json</c> column.</summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="value">The entity.</param>
        /// <returns>The serialised entity.</returns>
        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

        /// <summary>Deserialises an entity from the <c>Json</c> column.</summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="json">The serialised entity.</param>
        /// <returns>The entity, or <c>null</c> when the column is empty.</returns>
        public static T? Deserialize<T>(string? json) =>
            string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, Options);

        /// <summary>Measures the encoded size of a serialised entity.</summary>
        /// <param name="json">The serialised entity.</param>
        /// <returns>The size in bytes.</returns>
        /// <remarks>
        /// Used to decide whether a row must spill to a blob. A Table string property
        /// caps at 64 KB measured in bytes, not characters, so this must not use
        /// <see cref="string.Length"/>.
        /// </remarks>
        public static int ByteSize(string json) => Encoding.UTF8.GetByteCount(json);

        /// <summary>
        /// Builds a table row from an entity: the serialised entity in the <c>Json</c>
        /// column, plus whatever columns the caller needs to be able to filter on.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <param name="value">The entity to serialise.</param>
        /// <param name="columns">Adds the broken-out queryable columns, if any.</param>
        /// <returns>The row, ready to upsert.</returns>
        public static TableEntity ToEntity<T>(
            string partitionKey,
            string rowKey,
            T value,
            Action<TableEntity>? columns = null)
        {
            var entity = new TableEntity(partitionKey, rowKey)
            {
                { JsonColumn, Serialize(value) },
                { SchemaVersionColumn, CurrentSchemaVersion },
            };

            columns?.Invoke(entity);
            return entity;
        }

        /// <summary>Reads the entity back out of a row's <c>Json</c> column.</summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">The row.</param>
        /// <returns>The entity, or <c>null</c> when the row carries no payload.</returns>
        public static T? FromEntity<T>(TableEntity entity) =>
            Deserialize<T>(entity.GetString(JsonColumn));

        /// <summary>
        /// Drains a table query into a list.
        /// </summary>
        /// <param name="client">The table to query.</param>
        /// <param name="filter">An OData filter, or <c>null</c> for everything.</param>
        /// <param name="select">Columns to return, or <c>null</c> for all of them.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The matching entities.</returns>
        /// <remarks>
        /// Table Storage pages rather than offsets, and its filter grammar has no
        /// substring operator, so text search and paging happen in memory on the caller
        /// side. Passing <paramref name="select"/> is how that stays affordable: it keeps
        /// large columns off the wire when only keys or dates are needed.
        /// </remarks>
        public static async Task<List<TableEntity>> QueryAsync(
            TableClient client,
            string? filter = null,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<TableEntity>();
            var query = client.QueryAsync<TableEntity>(
                filter: filter,
                select: select,
                cancellationToken: cancellationToken);

            await foreach (var entity in query.WithCancellation(cancellationToken))
            {
                results.Add(entity);
            }

            return results;
        }

        /// <summary>
        /// Reads a single entity, returning <c>null</c> rather than throwing when absent.
        /// </summary>
        /// <param name="client">The table to read from.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The entity, or <c>null</c>.</returns>
        public static async Task<TableEntity?> GetIfExistsAsync(
            TableClient client,
            string partitionKey,
            string rowKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await client.GetEntityAsync<TableEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes entities in transactional batches.
        /// </summary>
        /// <param name="client">The table to delete from.</param>
        /// <param name="partitionKey">The partition every row shares.</param>
        /// <param name="rowKeys">The rows to delete.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        /// <remarks>
        /// A transaction is limited to 100 entities and cannot span partitions, so this
        /// chunks accordingly. Each chunk is atomic; the sequence of chunks is not.
        /// </remarks>
        public static async Task DeleteBatchAsync(
            TableClient client,
            string partitionKey,
            IEnumerable<string> rowKeys,
            CancellationToken cancellationToken = default)
        {
            foreach (var chunk in rowKeys.Chunk(100))
            {
                var actions = chunk
                    .Select(rowKey => new TableTransactionAction(
                        TableTransactionActionType.Delete,
                        new TableEntity(partitionKey, rowKey) { ETag = ETag.All }))
                    .ToList();

                if (actions.Count > 0)
                {
                    await client.SubmitTransactionAsync(actions, cancellationToken);
                }
            }
        }

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                // camelCase so a stored row reads exactly like an API payload, which
                // makes rows debuggable and keeps the archive format familiar. This is
                // as frozen as the key derivations: changing it later would leave every
                // existing row's properties unreadable under the new names.
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

                // Belt and braces, so a row written under a different convention still reads.
                PropertyNameCaseInsensitive = true,

                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { RelaxRequiredProperties },
                },
            };
            options.Converters.Add(new UtcDateTimeJsonConverter());
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
            return options;
        }

        /// <summary>
        /// Drops required-property enforcement when reading stored rows.
        /// </summary>
        /// <remarks>
        /// The entity models mark several properties <c>[JsonRequired]</c> or C#
        /// <c>required</c>, which is right for the HTTP contract: a client that omits
        /// <c>ShowMap</c> or <c>DiaryId</c> should be rejected. Applied to stored rows it
        /// is actively harmful. There is no migration step here, so the only way an older
        /// row survives a change in shape is by falling back to a default — and a
        /// required property turns that into a hard deserialisation failure instead,
        /// making any future field rename or removal a data-loss event rather than a
        /// no-op. The values are written by this application, not by a client, so there
        /// is nothing to validate on the way back in.
        /// </remarks>
        /// <param name="typeInfo">The type being configured.</param>
        private static void RelaxRequiredProperties(JsonTypeInfo typeInfo)
        {
            foreach (var property in typeInfo.Properties)
            {
                property.IsRequired = false;
            }
        }
    }
}
