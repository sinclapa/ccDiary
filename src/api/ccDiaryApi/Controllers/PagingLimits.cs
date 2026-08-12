// <copyright file="PagingLimits.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers
{
    /// <summary>
    /// Bounds for paged endpoint query parameters.
    /// </summary>
    /// <remarks>
    /// Paged queries filter in memory, so an unbounded <c>pageSize</c> lets a single
    /// request materialise an entire table. Callers asking for more than
    /// <see cref="MaxPageSize"/> get <see cref="MaxPageSize"/> rather than an error.
    /// </remarks>
    public static class PagingLimits
    {
        /// <summary>The largest page a caller may request.</summary>
        public const int MaxPageSize = 100;

        /// <summary>Clamps a requested page number to 1 or greater.</summary>
        /// <param name="page">The requested page number.</param>
        /// <returns>The page number, floored at 1.</returns>
        public static int ClampPage(int page) => page < 1 ? 1 : page;

        /// <summary>Clamps a requested page size to the range 1..<see cref="MaxPageSize"/>.</summary>
        /// <param name="pageSize">The requested page size.</param>
        /// <returns>The page size, bounded to the permitted range.</returns>
        public static int ClampPageSize(int pageSize)
        {
            if (pageSize < 1)
            {
                return 1;
            }

            return pageSize > MaxPageSize ? MaxPageSize : pageSize;
        }
    }
}
