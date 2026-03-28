// <copyright file="DiaryDTO.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Diary")]
    public class DiaryDTO
    {
        [Key]
        required public Guid DiaryId { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Length must not exceed 50 characters")]
        [MinLength(5, ErrorMessage = "Length must be at least 5 characters")]
        required public string Title { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Length must not exceed 50 characters")]
        required public string Author { get; set; }

        public string? Description { get; set; }
    }
}
