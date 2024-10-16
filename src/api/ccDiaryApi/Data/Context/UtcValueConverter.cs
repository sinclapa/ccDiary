// <copyright file="UtcValueConverter.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class UtcValueConverter : ValueConverter<DateTime, DateTime>
{
    public UtcValueConverter()
        : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}