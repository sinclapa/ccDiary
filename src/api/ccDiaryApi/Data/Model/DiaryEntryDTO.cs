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
        /// <summary>The most images one entry may hold; every one is returned inline on read.</summary>
        public const int MaxImages = 10;

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
        [JsonRequired]
        public bool ShowJourney { get; set; }

        public JourneyMode JourneyMode { get; set; } = JourneyMode.CrowFlies;

        /// <summary>
        /// Gets or sets the entry's first image. Kept alongside <see cref="Images"/> for archives
        /// and callers that predate it: a request that sends only this is a one-image entry, and
        /// every response repeats <see cref="Images"/>' first item here.
        /// </summary>
        public string? ImageData { get; set; }

        /// <summary>Gets or sets the media type of <see cref="ImageData"/>.</summary>
        public string? ImageContentType { get; set; }

        /// <summary>
        /// Gets or sets the entry's images, in display order. When present on a request it is
        /// authoritative, so an empty list removes them all; when absent,
        /// <see cref="ImageData"/> decides.
        /// </summary>
        [MaxLength(MaxImages)]
        public List<DiaryEntryImageDTO>? Images { get; set; }

        [ForeignKey(nameof(DiaryDTO))]
        required public Guid DiaryId { get; set; }

        [JsonIgnore]
        public DiaryDTO? Diary { get; set; }
    }
}
