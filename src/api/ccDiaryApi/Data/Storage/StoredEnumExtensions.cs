// <copyright file="StoredEnumExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using System.Text.Json;

    /// <summary>
    /// Converts an enum to the exact string form used inside the <c>Json</c> column.
    /// </summary>
    /// <remarks>
    /// Some enums are also broken out into their own column so they can be filtered on.
    /// Deriving that column's value from the same serializer, rather than writing the
    /// literal by hand, means the filter can never silently stop matching the payload if
    /// the naming policy or an enum member changes.
    /// </remarks>
    public static class StoredEnumExtensions
    {
        /// <summary>Renders an enum value as it is stored.</summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The stored representation, without surrounding quotes.</returns>
        public static string ToStoredValue<T>(this T value)
            where T : struct, Enum
        {
            return JsonSerializer.Serialize(value, TableJson.Options).Trim('"');
        }
    }
}
