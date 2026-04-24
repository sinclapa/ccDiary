// <copyright file="GeocodingCacheDto.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("GeocodingCache")]
    public class GeocodingCacheDto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        required public string Query { get; set; }

        public double Lat { get; set; }

        public double Lon { get; set; }

        public DateTime CachedAt { get; set; }
    }
}
