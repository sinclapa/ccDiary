// <copyright file="MapTileCacheDto.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MapTileCache")]
    public class MapTileCacheDto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        required public string Source { get; set; }

        public int Z { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        [Required]
        required public byte[] TileData { get; set; }

        [Required]
        [MaxLength(50)]
        required public string ContentType { get; set; }

        public DateTime CachedAt { get; set; }
    }
}
