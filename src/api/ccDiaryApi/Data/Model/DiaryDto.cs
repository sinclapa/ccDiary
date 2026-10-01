// <copyright file="DiaryDto.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Diary")]
    public class DiaryDto
    {
        [Key]
        public Guid? DiaryId { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Length must not exceed 50 characters")]
        [MinLength(5, ErrorMessage = "Length must be at least 5 characters")]
        required public string Title { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Length must not exceed 50 characters")]
        required public string Author { get; set; }

        public string? Description { get; set; }

        public string? OwnerId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the diary is hidden from public view.
        /// </summary>
        /// <remarks>
        /// A hidden diary, and every entry in it, can be read only by an admin or the contributor
        /// who owns it; anyone else gets 404, as if it did not exist. See <c>IDiaryVisibility</c>. A row
        /// written before this existed reads back as false, so nothing becomes hidden on its own.
        /// </remarks>
        public bool IsHidden { get; set; }
    }
}
