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

        public DbSet<AppUserDTO> AppUsers { get; set; }

        public DbSet<AccessRequestDTO> AccessRequests { get; set; }

        public DbSet<MapTileCacheDTO> MapTileCache { get; set; }

        public DbSet<GeocodingCacheDTO> GeocodingCache { get; set; }

        public DbSet<RoutingCacheDTO> RoutingCache { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUserDTO>()
                .HasIndex(u => u.EntraObjectId)
                .IsUnique();

            modelBuilder.Entity<MapTileCacheDTO>()
                .HasIndex(t => new { t.Source, t.Z, t.X, t.Y })
                .IsUnique();

            modelBuilder.Entity<GeocodingCacheDTO>()
                .HasIndex(g => g.Query)
                .IsUnique();

            modelBuilder.Entity<RoutingCacheDTO>()
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
