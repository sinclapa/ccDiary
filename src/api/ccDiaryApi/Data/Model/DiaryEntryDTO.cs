using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ccDiaryApi.Data.Model
{
    [Table("DiaryEntry")]
    public class DiaryEntryDTO
    {
        [Key][Required]
        public Guid DiaryEntryId { get; set; }

        [Required]
        public DateTime Date { get; set; }
 
        [Required]
        public string? Location { get; set; }

        [Required]
        public string? Entry { get; set; }

        [ForeignKey(nameof(DiaryDTO))]
        public Guid DiaryId { get; set; }

        public DiaryDTO? Diary { get; set; }
    }
}
