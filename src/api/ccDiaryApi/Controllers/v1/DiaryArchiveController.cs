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
        private readonly IDiaryArchiveService _diaryArchiveService;

        public DiaryArchiveController(IDiaryService diaryService, IDiaryEntryService diaryEntryService, IDiaryArchiveService diaryArchiveService)
        {
            _diaryService = diaryService;
            _diaryEntryService = diaryEntryService;
            _diaryArchiveService = diaryArchiveService;
        }

        [Route("{diaryId:guid}")]
        [HttpGet]
        public ActionResult<DiaryArchiveDTO> Export(Guid diaryId)
        {
            var diary = _diaryService.GetDiary(diaryId);
            if (diary == null)
            {
                return NotFound();
            }

            var diaryEntries = _diaryEntryService.GetDiaryEntries(diaryId);
            DiaryArchiveDTO export = new () { Diary = diary, DiaryEntries = diaryEntries };
            return Ok(export);
        }

        [HttpPost]
        public ActionResult<DiaryDTO> Import(DiaryArchiveDTO diaryArchive)
        {
            var diary = _diaryArchiveService.Import(diaryArchive);
            return Ok(diary);
        }
    }
}
