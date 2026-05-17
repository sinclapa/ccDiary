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
        public async Task<ActionResult<IEnumerable<AccessRequestDto>>> Requests()
        {
            var requests = await _accessRequestService.GetAllAsync();
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

        [HttpDelete]
        [Route("{requestId:guid}")]
        public async Task<ActionResult> Delete(Guid requestId)
        {
            try
            {
                await _accessRequestService.DeleteAsync(requestId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("{requestId:guid}")]
        public async Task<ActionResult> ResendInvitation(Guid requestId)
        {
            try
            {
                var redeemUrl = await _accessRequestService.ResendInvitationAsync(requestId);
                if (redeemUrl == null)
                {
                    return BadRequest(new { message = "No invitation link available for this request." });
                }

                return Ok(new { redeemUrl });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
