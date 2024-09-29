using ccDiaryApi.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ccDiaryApi.Data.Context
{
    public class DiaryDatabaseContext : DbContext
    {
        public DiaryDatabaseContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<DiaryDTO> Diaries { get; set; }

        public DbSet<DiaryEntryDTO> DiaryEntries { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<DateTime>()
                .HaveConversion(typeof(UtcValueConverter));
        }
    }
}
