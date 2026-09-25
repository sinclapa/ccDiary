// <copyright file="DiaryEntryImageDTO.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>One image attached to a diary entry, carried inline as base64.</summary>
    public class DiaryEntryImageDTO
    {
        /// <summary>Gets or sets the image bytes, base64 encoded.</summary>
        [Required]
        public string? Data { get; set; }

        /// <summary>Gets or sets the image's media type, e.g. <c>image/jpeg</c>.</summary>
        [Required]
        public string? ContentType { get; set; }
    }
}
