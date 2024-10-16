// <copyright file="DiaryEntryDTO.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    [Table("DiaryEntry")]
    public class DiaryEntryDTO
    {
        [Key]
        [Required]
        public Guid DiaryEntryId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string? Location { get; set; }

        [Required]
        public string? Entry { get; set; }

        [ForeignKey(nameof(DiaryDTO))]
        public Guid DiaryId { get; set; }

        [JsonIgnore]
        public DiaryDTO? Diary { get; set; }
    }
}
