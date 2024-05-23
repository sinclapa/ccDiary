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
    }
}
