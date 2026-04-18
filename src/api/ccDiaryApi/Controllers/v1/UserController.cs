// <copyright file="UserController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using Asp.Versioning;
    using ccDiaryApi.Extensions;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult> Me()
        {
            var oid = User.GetOid();
            if (string.IsNullOrEmpty(oid))
            {
                return Unauthorized();
            }

            var email = User.FindFirst("preferred_username")?.Value
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                     ?? string.Empty;
            var displayName = User.FindFirst("name")?.Value
                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                           ?? email;

            var appUser = await _userService.GetOrCreateUserAsync(oid, email, displayName);
            if (appUser == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                appUser.UserId,
                appUser.DisplayName,
                appUser.Email,
                appUser.Role,
                EntraObjectId = oid,
            });
        }
    }
}
