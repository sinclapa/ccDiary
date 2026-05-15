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
        public ActionResult<PagedResultDTO<DiaryDTO>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var diaries = _diaryService.GetDiaries(page, pageSize);
            return Ok(diaries);
        }

        [Route("{diaryId:guid}")]
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<DiaryDTO> Get(Guid diaryId)
        {
            var diary = _diaryService.GetDiary(diaryId);
            return Ok(diary);
        }

        [HttpPost]
        [Authorize(Policy = "DiaryContributor")]
        public ActionResult<DiaryDTO> Create([FromBody] DiaryDTO diary)
        {
            diary.OwnerId = User.GetOid();
            var retDiary = _diaryService.Create(diary);
            _logger.LogInformation("Diary created. DiaryId={DiaryId}", SanitizeForLog(retDiary.DiaryId));
            return Created("Uri", retDiary);
        }

        [HttpPut]
        [Authorize(Policy = "DiaryContributor")]
        public ActionResult<DiaryDTO> Update([FromBody] DiaryDTO diary)
        {
            if (!User.IsInRole("DiaryAdmin"))
            {
                var existing = _diaryService.GetDiary(diary.DiaryId ?? Guid.Empty);
                if (existing == null || existing.OwnerId != User.GetOid())
                {
                    return Forbid();
                }
            }

            var retDiary = _diaryService.Update(diary);
            _logger.LogInformation("Diary updated. DiaryId={DiaryId}", SanitizeForLog(retDiary.DiaryId));
            return Ok(retDiary);
        }

        [Route("{diaryId:guid}")]
        [HttpDelete]
        [Authorize(Policy = "DiaryContributor")]
        public ActionResult Delete(Guid diaryId)
        {
            var diary = _diaryService.GetDiary(diaryId);
            if (diary == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("DiaryAdmin") && diary.OwnerId != User.GetOid())
            {
                return Forbid();
            }

            _diaryService.Delete(diary);
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
