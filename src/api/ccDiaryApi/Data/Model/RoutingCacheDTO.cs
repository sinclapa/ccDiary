// <copyright file="RoutingCacheDTO.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("RoutingCache")]
    public class RoutingCacheDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public double FromLat { get; set; }

        public double FromLon { get; set; }

        public double ToLat { get; set; }

        public double ToLon { get; set; }

        [Required]
        [MaxLength(10)]
        required public string Profile { get; set; }

        [Required]
        required public string RouteCoords { get; set; }

        public DateTime CachedAt { get; set; }
    }
}
