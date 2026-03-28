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
        private readonly IDiaryArchiveService _diaryArchiveService;

        public DiaryArchiveController(IDiaryArchiveService diaryArchiveService)
        {
            _diaryArchiveService = diaryArchiveService;
        }

        [Route("{diaryId:guid}")]
        [HttpGet]
        public ActionResult<DiaryArchiveDTO> Export(Guid diaryId)
        {
            var export = _diaryArchiveService.Export(diaryId);
            if (export == null)
            {
                return NotFound();
            }

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
