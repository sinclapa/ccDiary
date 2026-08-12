// <copyright file="DiaryController.cs" company="CookingCode">
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
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class DiaryController : ControllerBase
    {
        private readonly IDiaryService _diaryService;
        private readonly ILogger<DiaryController> _logger;

        public DiaryController(IDiaryService diaryService, ILogger<DiaryController>? logger = null)
        {
            _diaryService = diaryService;
            _logger = logger ?? NullLogger<DiaryController>.Instance;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResultDTO<DiaryDTO>>> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? search = null)
        {
            var diaries = await _diaryService.GetDiariesAsync(
                PagingLimits.ClampPage(page),
                PagingLimits.ClampPageSize(pageSize),
                search);
            return Ok(diaries);
        }

        [Route("{diaryId:guid}")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<DiaryDTO>> Get(Guid diaryId)
        {
            var diary = await _diaryService.GetDiaryAsync(diaryId);
            return Ok(diary);
        }

        [HttpPost]
        [Authorize(Policy = "DiaryContributor")]
        public async Task<ActionResult<DiaryDTO>> Create([FromBody] DiaryDTO diary)
        {
            diary.OwnerId = User.GetOid();
            var retDiary = await _diaryService.CreateAsync(diary);
            _logger.LogInformation("Diary created. DiaryId={DiaryId}", SanitizeForLog(retDiary.DiaryId));
            return Created("Uri", retDiary);
        }

        [HttpPut]
        [Authorize(Policy = "DiaryContributor")]
        public async Task<ActionResult<DiaryDTO>> Update([FromBody] DiaryDTO diary)
        {
            if (!User.IsInRole("DiaryAdmin"))
            {
                var existing = await _diaryService.GetDiaryAsync(diary.DiaryId ?? Guid.Empty);
                if (existing == null || existing.OwnerId != User.GetOid())
                {
                    return Forbid();
                }
            }

            var retDiary = await _diaryService.UpdateAsync(diary);
            _logger.LogInformation("Diary updated. DiaryId={DiaryId}", SanitizeForLog(retDiary.DiaryId));
            return Ok(retDiary);
        }

        [Route("{diaryId:guid}")]
        [HttpDelete]
        [Authorize(Policy = "DiaryContributor")]
        public async Task<ActionResult> Delete(Guid diaryId)
        {
            var diary = await _diaryService.GetDiaryAsync(diaryId);
            if (diary == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("DiaryAdmin") && diary.OwnerId != User.GetOid())
            {
                return Forbid();
            }

            await _diaryService.DeleteAsync(diary);
            _logger.LogInformation("Diary deleted. DiaryId={DiaryId}", SanitizeForLog(diaryId));
            return Ok();
        }

        private static string SanitizeForLog(object? value)
        {
            var s = value?.ToString() ?? string.Empty;
            return s.Replace("\r", string.Empty)
                    .Replace("\n", string.Empty);
        }
    }
}
