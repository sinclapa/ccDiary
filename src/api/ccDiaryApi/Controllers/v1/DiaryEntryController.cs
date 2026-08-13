// <copyright file="DiaryEntryController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using System.ComponentModel;
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
    public class DiaryEntryController : ControllerBase
    {
        private readonly IDiaryEntryService _diaryEntryService;
        private readonly IDiaryService _diaryService;
        private readonly ILogger<DiaryEntryController> _logger;

        public DiaryEntryController(
            IDiaryEntryService diaryEntryService,
            IDiaryService diaryService,
            ILogger<DiaryEntryController>? logger = null)
        {
            _diaryEntryService = diaryEntryService;
            _diaryService = diaryService;
            _logger = logger ?? NullLogger<DiaryEntryController>.Instance;
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<int>>> Search(Guid diaryId)
        {
            var range = await _diaryEntryService.GetDiaryDateRangeAsync(diaryId);
            var years = await _diaryEntryService.SearchDiaryEntriesAsync(diaryId, range.MinDateTime, range.MaxDateTime, SearchType.Year);
            return Ok(years);
        }

        [Route("{diaryId:guid}/{year:int}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<int>>> Search(Guid diaryId, int year)
        {
            var from = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddYears(1).Subtract(new TimeSpan(1));
            var months = await _diaryEntryService.SearchDiaryEntriesAsync(diaryId, from, to, SearchType.Month);
            return Ok(months);
        }

        [Route("{diaryId:guid}/{year:int}/{month:int}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<int>>> Search(Guid diaryId, int year, int month, [FromHeader(Name = "x-utc-offset")][DefaultValue(0)] int utcOffsetMinutes)
        {
            var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(-1 * utcOffsetMinutes);
            var to = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddMinutes(-1 * utcOffsetMinutes).Subtract(new TimeSpan(1));
            var days = await _diaryEntryService.SearchDiaryEntriesAsync(diaryId, from, to, SearchType.Day, utcOffsetMinutes);
            return Ok(days);
        }

        [Route("{diaryId:guid}/{year:int}/{month:int}/{day:int}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<DiaryEntryDTO>>> Search(Guid diaryId, int year, int month, int day, [FromHeader(Name = "x-utc-offset")][DefaultValue(0)] int utcOffsetMinutes)
        {
            var from = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddMinutes(-1 * utcOffsetMinutes);
            var to = from.AddDays(1).Subtract(new TimeSpan(1));
            var searchResult = await _diaryEntryService.GetDiaryEntriesAsync(diaryId, from.ToUniversalTime(), to.ToUniversalTime());
            return Ok(searchResult);
        }

        [Route("{diaryEntryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<DiaryEntryDTO>> Get(Guid diaryEntryId)
        {
            var diaryEntry = await _diaryEntryService.GetDiaryEntryAsync(diaryEntryId);
            if (diaryEntry == null)
            {
                return NotFound();
            }

            return Ok(diaryEntry);
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DiaryEntryDTO>>> GetDiaryEntries(Guid diaryId)
        {
            var diaryEntries = await _diaryEntryService.GetDiaryEntriesAsync(diaryId);
            return Ok(diaryEntries);
        }

        [HttpPost]
        [Authorize(Policy = "DiaryContributor")]
        [RequestSizeLimit(RequestLimits.DiaryEntryBytes)]
        public async Task<ActionResult<DiaryEntryDTO>> Create([FromBody] DiaryEntryDTO diaryEntry)
        {
            if (User.IsInRole("DiaryAdmin"))
            {
                // A foreign key used to reject an entry pointing at a diary that does not
                // exist, surfacing as a 500. There is no referential integrity behind a
                // key-value store, so the check is explicit — and reported as the client
                // error it always was.
                if (await _diaryService.GetDiaryAsync(diaryEntry.DiaryId) == null)
                {
                    return BadRequest($"Diary {diaryEntry.DiaryId} does not exist.");
                }
            }
            else
            {
                var diary = await _diaryService.GetDiaryAsync(diaryEntry.DiaryId);
                if (diary == null || diary.OwnerId != User.GetOid())
                {
                    return Forbid();
                }
            }

            var retDiaryEntry = await _diaryEntryService.CreateDiaryEntryAsync(diaryEntry);
            _logger.LogInformation(
                "Diary entry created. DiaryEntryId={DiaryEntryId} DiaryId={DiaryId}",
                SanitizeForLog(retDiaryEntry.DiaryEntryId),
                SanitizeForLog(retDiaryEntry.DiaryId));
            return Created("URI", retDiaryEntry);
        }

        [HttpPut]
        [Authorize(Policy = "DiaryContributor")]
        [RequestSizeLimit(RequestLimits.DiaryEntryBytes)]
        public async Task<ActionResult<DiaryEntryDTO>> Update([FromBody] DiaryEntryDTO diaryEntry)
        {
            if (!User.IsInRole("DiaryAdmin"))
            {
                var diary = await _diaryService.GetDiaryAsync(diaryEntry.DiaryId);
                if (diary == null || diary.OwnerId != User.GetOid())
                {
                    return Forbid();
                }
            }

            var retDiaryEntry = await _diaryEntryService.UpdateDiaryEntryAsync(diaryEntry);
            _logger.LogInformation(
                "Diary entry updated. DiaryEntryId={DiaryEntryId} DiaryId={DiaryId}",
                SanitizeForLog(retDiaryEntry.DiaryEntryId),
                SanitizeForLog(retDiaryEntry.DiaryId));
            return Ok(retDiaryEntry);
        }

        [Route("{diaryEntryId:guid}")]
        [HttpDelete]
        [Authorize(Policy = "DiaryContributor")]
        public async Task<ActionResult> Delete(Guid diaryEntryId)
        {
            var diaryEntry = await _diaryEntryService.GetDiaryEntryAsync(diaryEntryId);
            if (diaryEntry == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("DiaryAdmin"))
            {
                var diary = await _diaryService.GetDiaryAsync(diaryEntry.DiaryId);
                if (diary == null || diary.OwnerId != User.GetOid())
                {
                    return Forbid();
                }
            }

            await _diaryEntryService.DeleteDiaryEntryAsync(diaryEntry);
            _logger.LogInformation(
                "Diary entry deleted. DiaryEntryId={DiaryEntryId}",
                SanitizeForLog(diaryEntryId));
            return Ok();
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PagedResultDTO<DiaryEntryDTO>>> TextSearch(
            Guid diaryId,
            [FromQuery] string search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return BadRequest("search is required");
            }

            var results = await _diaryEntryService.TextSearchDiaryEntriesAsync(
                diaryId,
                search,
                PagingLimits.ClampPage(page),
                PagingLimits.ClampPageSize(pageSize));
            return Ok(results);
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<DateTime>> GetMinDate(Guid diaryId)
        {
            var date = await _diaryEntryService.MinDiaryEntryDateAsync(diaryId);
            return Ok(date);
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<DateTime>> GetMaxDate(Guid diaryId)
        {
            var date = await _diaryEntryService.MaxDiaryEntryDateAsync(diaryId);
            return Ok(date);
        }

        private static string SanitizeForLog(object? value)
        {
            var s = value?.ToString() ?? string.Empty;
            return s.Replace("\r", string.Empty)
                    .Replace("\n", string.Empty);
        }
    }
}
