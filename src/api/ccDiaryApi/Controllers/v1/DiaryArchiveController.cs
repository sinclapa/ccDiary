// <copyright file="DiaryArchiveController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using Asp.Versioning;
    using ccDiaryApi.Authorization;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<DiaryArchiveDTO>> Export(Guid diaryId)
        {
            _logger.LogInformation("Export requested. DiaryId={DiaryId}", SanitizeForLog(diaryId));

            var export = await _diaryArchiveService.ExportAsync(diaryId);
            if (export == null)
            {
                _logger.LogWarning("Export not found. DiaryId={DiaryId}", SanitizeForLog(diaryId));
                return NotFound();
            }

            _logger.LogInformation(
                "Export succeeded. DiaryId={DiaryId} EntryCount={EntryCount}",
                SanitizeForLog(diaryId),
                SanitizeForLog(export.DiaryEntries?.Count));
            return Ok(export);
        }

        /// <summary>
        /// Loads a whole diary and its entries, creating or replacing them by their own ids.
        /// </summary>
        /// <remarks>
        /// An import can overwrite any diary whose id it names, so outside the local environments
        /// it needs the <see cref="ArchiveImportRequirement"/> policy - an admin, or the
        /// application's own app-only token, which is how the deploy pipeline seeds test data. A
        /// caller who is not signed in gets 401, and one who is signed in otherwise gets 403.
        /// Locally it stays open, so a developer can load an archive without a token.
        /// </remarks>
        /// <param name="env">The hosting environment, which decides whether the admin check applies.</param>
        /// <param name="authorization">Evaluates the archive import policy for the caller.</param>
        /// <param name="diaryArchive">The diary and its entries.</param>
        /// <returns>The imported diary; 401 or 403 when the caller may not import.</returns>
        [HttpPost]
        [AllowAnonymous]
        [RequestSizeLimit(RequestLimits.ArchiveImportBytes)]
        public async Task<ActionResult<DiaryDTO>> Import(
            [FromServices] IWebHostEnvironment env,
            [FromServices] IAuthorizationService authorization,
            DiaryArchiveDTO diaryArchive)
        {
            bool isLocalEnvironment = env.IsEnvironment("local")
                || env.IsEnvironment("LocalContainer")
                || env.IsEnvironment("LocalCompose");

            if (!isLocalEnvironment)
            {
                if (!(User.Identity?.IsAuthenticated ?? false))
                {
                    return Unauthorized();
                }

                var allowed = await authorization.AuthorizeAsync(User, ArchiveImportRequirement.PolicyName);
                if (!allowed.Succeeded)
                {
                    return Forbid();
                }
            }

            var diary = await _diaryArchiveService.ImportAsync(diaryArchive);
            _logger.LogInformation(
                "Import succeeded. DiaryId={DiaryId} EntryCount={EntryCount}",
                SanitizeForLog(diary.DiaryId),
                SanitizeForLog(diaryArchive?.DiaryEntries?.Count));
            return Ok(diary);
        }

        private static string SanitizeForLog(object? value)
        {
            var s = value?.ToString() ?? string.Empty;
            return s.Replace("\r", string.Empty)
                .Replace("\n", string.Empty);
        }
    }
}
