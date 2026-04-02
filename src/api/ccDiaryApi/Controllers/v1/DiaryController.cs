// <copyright file="DiaryController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using Asp.Versioning;
    using ccDiaryApi.Data.Model;
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
        public ActionResult<IEnumerable<DiaryDTO>> Get()
        {
            var diaries = _diaryService.GetDiaries();
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
        public ActionResult<DiaryDTO> Create([FromBody] DiaryDTO diary)
        {
            var retDiary = _diaryService.Create(diary);
            _logger.LogInformation("Diary created. DiaryId={DiaryId}", retDiary.DiaryId);
            return Created("Uri", retDiary);
        }

        [HttpPut]
        public ActionResult<DiaryDTO> Update([FromBody] DiaryDTO diary)
        {
            var retDiary = _diaryService.Update(diary);
            _logger.LogInformation("Diary updated. DiaryId={DiaryId}", retDiary.DiaryId);
            return Ok(retDiary);
        }

        [Route("{diaryId:guid}")]
        [HttpDelete]
        public ActionResult Delete(Guid diaryId)
        {
            var diary = _diaryService.GetDiary(diaryId);
            if (diary == null)
            {
                return NotFound();
            }

            _diaryService.Delete(diary);
            _logger.LogInformation("Diary deleted. DiaryId={DiaryId}", diaryId);
            return Ok();
        }
    }
}
