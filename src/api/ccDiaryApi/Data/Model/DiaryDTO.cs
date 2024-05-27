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
        [MaxLength(50, ErrorMessage = "Length must not exceed 50 characters")]
        [MinLength(5, ErrorMessage = "Length must be at least 5 characters")]
        public required string Title { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Length must not exceed 50 characters")]
        public required string Author { get; set; }

        public string? Description { get; set; } 
    }
}
