// <copyright file="AppUserDTO.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("AppUser")]
    public class AppUserDTO
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        required public string EntraObjectId { get; set; }

        [Required]
        [MaxLength(100)]
        required public string DisplayName { get; set; }

        [Required]
        [MaxLength(200)]
        required public string Email { get; set; }

        public AppRole Role { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
