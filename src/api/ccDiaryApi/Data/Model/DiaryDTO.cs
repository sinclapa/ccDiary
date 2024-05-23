using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ccDiaryApi.Data.Model
{
    [Table("Diary")]
    public class DiaryDTO
    {
        [Key]
        public Guid DiaryId { get; set; }

        [Required]
        public required string Name { get; set; }

        public string? Description { get; set; } 
    }
}
