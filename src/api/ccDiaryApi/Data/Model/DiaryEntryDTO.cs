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
        public Guid? DiaryEntryId { get; set; }

        public DateTime? Date { get; set; }

        [Required]
        public string? Location { get; set; }

        [Required]
        public string? Entry { get; set; }

        public string? MapLocation { get; set; }

        [Required]
        [JsonRequired]
        public bool ShowMap { get; set; }

        public string? FromLocation { get; set; }

        public string? ToLocation { get; set; }

        [Required]
        public bool ShowJourney { get; set; }

        [ForeignKey(nameof(DiaryDTO))]
        required public Guid DiaryId { get; set; }

        [JsonIgnore]
        public DiaryDTO? Diary { get; set; }
    }
}
