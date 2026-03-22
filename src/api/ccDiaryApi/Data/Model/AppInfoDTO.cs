// <copyright file="AppInfoDTO.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("AppInfo")]
    public class AppInfoDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; } = 1;

        [Required]
        [MaxLength(100)]
        required public string InformationalVersion { get; set; }

        [Required]
        public DateTime DatabaseLastUpdated { get; set; }
    }
}
