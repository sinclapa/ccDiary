// <copyright file="AccessRequestDto.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("AccessRequest")]
    public class AccessRequestDto
    {
        [Key]
        public Guid AccessRequestId { get; set; }

        [Required]
        [MaxLength(100)]
        required public string DisplayName { get; set; }

        [Required]
        [MaxLength(200)]
        required public string Email { get; set; }

        public RequestStatus Status { get; set; }

        public DateTime RequestedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public Guid? ProcessedByUserId { get; set; }

        [ForeignKey(nameof(ProcessedByUserId))]
        public AppUserDto? ProcessedBy { get; set; }

        [MaxLength(2048)]
        public string? InviteRedeemUrl { get; set; }
    }
}
