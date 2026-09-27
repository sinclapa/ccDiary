// <copyright file="PagedResultDto.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    public class PagedResultDto<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}
