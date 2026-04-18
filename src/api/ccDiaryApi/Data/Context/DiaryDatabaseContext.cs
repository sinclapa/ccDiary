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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUserDTO>()
                .HasIndex(u => u.EntraObjectId)
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
