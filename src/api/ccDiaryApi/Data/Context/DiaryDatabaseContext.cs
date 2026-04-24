// <copyright file="DiaryDatabaseContext.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Context
{
    using ccDiaryApi.Data.Model;
    using Microsoft.EntityFrameworkCore;

    public class DiaryDatabaseContext : DbContext
    {
        public DiaryDatabaseContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<DiaryDTO> Diaries { get; set; }

        public DbSet<DiaryEntryDTO> DiaryEntries { get; set; }

        public DbSet<AppInfoDTO> AppInfo { get; set; }

        public DbSet<AppUserDto> AppUsers { get; set; }

        public DbSet<AccessRequestDto> AccessRequests { get; set; }

        public DbSet<MapTileCacheDto> MapTileCache { get; set; }

        public DbSet<GeocodingCacheDto> GeocodingCache { get; set; }

        public DbSet<RoutingCacheDto> RoutingCache { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUserDto>()
                .HasIndex(u => u.EntraObjectId)
                .IsUnique();

            modelBuilder.Entity<MapTileCacheDto>()
                .HasIndex(t => new { t.Source, t.Z, t.X, t.Y })
                .IsUnique();

            modelBuilder.Entity<GeocodingCacheDto>()
                .HasIndex(g => g.Query)
                .IsUnique();

            modelBuilder.Entity<RoutingCacheDto>()
                .HasIndex(r => new { r.FromLat, r.FromLon, r.ToLat, r.ToLon, r.Profile })
                .IsUnique();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<DateTime>()
                .HaveConversion(typeof(UtcValueConverter));
        }
    }
}
