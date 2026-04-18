// <copyright file="AccessRequestController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using Asp.Versioning;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    [AllowAnonymous]
    public class AccessRequestController : ControllerBase
    {
        private readonly IAccessRequestService _accessRequestService;

        public AccessRequestController(IAccessRequestService accessRequestService)
        {
            _accessRequestService = accessRequestService;
        }

        [HttpPost]
        public async Task<ActionResult> Submit([FromBody] SubmitAccessRequestBody body)
        {
            try
            {
                await _accessRequestService.SubmitAsync(body.displayName, body.email);
                return Created(string.Empty, null);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        public record SubmitAccessRequestBody(string displayName, string email);
    }
}
