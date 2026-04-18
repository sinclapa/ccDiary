// <copyright file="AdminController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using Asp.Versioning;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Extensions;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "DiaryAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IAccessRequestService _accessRequestService;

        public AdminController(IAccessRequestService accessRequestService)
        {
            _accessRequestService = accessRequestService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccessRequestDTO>>> Requests()
        {
            var requests = await _accessRequestService.GetPendingAsync();
            return Ok(requests);
        }

        [HttpPut]
        [Route("{requestId:guid}")]
        public async Task<ActionResult> Approve(Guid requestId)
        {
            var adminOid = User.GetOid();
            if (string.IsNullOrEmpty(adminOid))
            {
                return Unauthorized();
            }

            try
            {
                var redeemUrl = await _accessRequestService.ApproveAsync(requestId, adminOid);
                return Ok(new { redeemUrl });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPut]
        [Route("{requestId:guid}")]
        public async Task<ActionResult> Decline(Guid requestId)
        {
            var adminOid = User.GetOid();
            if (string.IsNullOrEmpty(adminOid))
            {
                return Unauthorized();
            }

            try
            {
                await _accessRequestService.DeclineAsync(requestId, adminOid);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
