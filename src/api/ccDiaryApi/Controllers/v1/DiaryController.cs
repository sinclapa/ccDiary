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

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class DiaryController : ControllerBase
    {
        private readonly IDiaryService _diaryService;
        public DiaryController(IDiaryService diaryService)
        {
            _diaryService = diaryService;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<DiaryDTO>> Get()
        {
            var diaries = _diaryService.Get();
            return Ok(diaries);
        }

        [Route("{diaryId:guid}")]
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<DiaryDTO> Get(Guid diaryId)
        {
            var diary = _diaryService.Get(diaryId);
            return Ok(diary);
        }

        [HttpPost]
        public ActionResult<DiaryDTO> Create([FromBody] DiaryDTO diary)
        {
            var retDiary = _diaryService.Create(diary);
            return Created("Uri", retDiary);
        }

        [HttpPut]
        public ActionResult<DiaryDTO> Update([FromBody] DiaryDTO diary)
        {
            var retDiary = _diaryService.Update(diary);
            return Ok(retDiary);
        }

        [Route("{diaryId:guid}")]
        [HttpDelete]
        public ActionResult Delete(Guid diaryId)
        {
            var diary = _diaryService.Get(diaryId);
            if (diary == null)
            {
                return NotFound();
            }

            _diaryService.Delete(diary);
            return Ok();
        }
    }
}
