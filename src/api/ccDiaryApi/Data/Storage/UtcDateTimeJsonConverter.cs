// <copyright file="UtcDateTimeJsonConverter.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Serialises <see cref="DateTime"/> values to storage as round-trippable UTC and
    /// reads them back with <see cref="DateTimeKind.Utc"/> set.
    /// </summary>
    /// <remarks>
    /// This replaces the EF <c>UtcValueConverter</c>. Values broken out into their own
    /// table columns come back as UTC already, but anything inside the serialised JSON
    /// column would otherwise deserialise as <see cref="DateTimeKind.Unspecified"/> and
    /// silently shift when a caller converted it. Registered only on the storage
    /// serializer options, never on the MVC ones, so the HTTP contract is untouched.
    /// </remarks>
    public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        /// <inheritdoc/>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

            writer.WriteStringValue(utc.ToString("O", CultureInfo.InvariantCulture));
        }
    }
}
