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
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class DiaryArchiveController : ControllerBase
    {
        private readonly IDiaryArchiveService _diaryArchiveService;
        private readonly ILogger<DiaryArchiveController> _logger;

        public DiaryArchiveController(IDiaryArchiveService diaryArchiveService, ILogger<DiaryArchiveController>? logger = null)
        {
            _diaryArchiveService = diaryArchiveService;
            _logger = logger ?? NullLogger<DiaryArchiveController>.Instance;
        }

        [Route("{diaryId:guid}")]
        [HttpGet]
        public ActionResult<DiaryArchiveDTO> Export(Guid diaryId)
        {
            _logger.LogInformation("Export requested. DiaryId={DiaryId}", diaryId);

            try
            {
                var export = _diaryArchiveService.Export(diaryId);
                if (export == null)
                {
                    _logger.LogWarning("Export not found. DiaryId={DiaryId}", diaryId);
                    return NotFound();
                }

                _logger.LogInformation("Export succeeded. DiaryId={DiaryId} EntryCount={EntryCount}", diaryId, export.DiaryEntries?.Count ?? 0);
                return Ok(export);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database dependency failure exporting diary archive. DiaryId={DiaryId}", diaryId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failure exporting diary archive. DiaryId={DiaryId}", diaryId);
                throw;
            }
        }

        [HttpPost]
        public ActionResult<DiaryDTO> Import(DiaryArchiveDTO diaryArchive)
        {
            try
            {
                var diary = _diaryArchiveService.Import(diaryArchive);
                _logger.LogInformation("Import succeeded. DiaryId={DiaryId} EntryCount={EntryCount}", diary.DiaryId, diaryArchive.DiaryEntries?.Count ?? 0);
                return Ok(diary);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database dependency failure importing diary archive. SourceDiaryId={DiaryId}", diaryArchive.Diary?.DiaryId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failure importing diary archive. SourceDiaryId={DiaryId}", diaryArchive.Diary?.DiaryId);
                throw;
            }
        }
    }
}
