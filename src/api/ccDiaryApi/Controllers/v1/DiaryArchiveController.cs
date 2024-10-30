// <copyright file="DiaryArchiveController.cs" company="CookingCode">
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
    public class DiaryArchiveController : ControllerBase
    {
        private readonly IDiaryService _diaryService;
        private readonly IDiaryEntryService _diaryEntryService;

        public DiaryArchiveController(IDiaryService diaryService, IDiaryEntryService diaryEntryService)
        {
            _diaryService = diaryService;
            _diaryEntryService = diaryEntryService;
        }

        [Route("{diaryId:guid}")]
        [HttpGet]
        public ActionResult<DiaryExportDTO> Export(Guid diaryId)
        {
            var diary = _diaryService.GetDiary(diaryId);
            if (diary == null)
            {
                return NotFound();
            }

            var diaryEntries = _diaryEntryService.GetDiaryEntries(diaryId);
            DiaryExportDTO export = new () { Diary = diary, DiaryEntries = diaryEntries };
            return Ok(export);
        }
    }
}
