// <copyright file="DiaryEntryController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using System.ComponentModel;
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
    public class DiaryEntryController : ControllerBase
    {
        private readonly IDiaryEntryService _diaryEntryService;
        private readonly ILogger<DiaryEntryController> _logger;

        public DiaryEntryController(IDiaryEntryService diaryEntryService, ILogger<DiaryEntryController>? logger = null)
        {
            _diaryEntryService = diaryEntryService;
            _logger = logger ?? NullLogger<DiaryEntryController>.Instance;
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<List<int>> Search(Guid diaryId)
        {
            var range = _diaryEntryService.GetDiaryDateRange(diaryId);
            var years = _diaryEntryService.SearchDiaryEntries(diaryId, range.MinDateTime, range.MaxDateTime, SearchType.Year);
            return Ok(years);
        }

        [Route("{diaryId:guid}/{year:int}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<List<int>> Search(Guid diaryId, int year)
        {
            var from = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddYears(1).Subtract(new TimeSpan(1));
            var months = _diaryEntryService.SearchDiaryEntries(diaryId, from, to, SearchType.Month);
            return Ok(months);
        }

        [Route("{diaryId:guid}/{year:int}/{month:int}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<List<int>> Search(Guid diaryId, int year, int month)
        {
            var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddMonths(1).Subtract(new TimeSpan(1));
            var days = _diaryEntryService.SearchDiaryEntries(diaryId, from, to, SearchType.Day);
            return Ok(days);
        }

        [Route("{diaryId:guid}/{year:int}/{month:int}/{day:int}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<List<DiaryEntryDTO>> Search(Guid diaryId, int year, int month, int day, [FromHeader(Name="x-utc-offset")][DefaultValue(0)] int utcOffsetMinutes)
        {
            var from = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddMinutes(-1 * utcOffsetMinutes);
            var to = from.AddDays(1).Subtract(new TimeSpan(1));
            var searchResult = _diaryEntryService.GetDiaryEntries(diaryId, from.ToUniversalTime(), to.ToUniversalTime());
            return Ok(searchResult);
        }

        [Route("{diaryEntryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<DiaryEntryDTO> Get(Guid diaryEntryId)
        {
            var diaryEntry = _diaryEntryService.GetDiaryEntry(diaryEntryId);
            if (diaryEntry == null)
            {
                return NotFound();
            }

            return Ok(diaryEntry);
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<DiaryEntryDTO>> GetDiaryEntries(Guid diaryId)
        {
            var diaryEntries = _diaryEntryService.GetDiaryEntries(diaryId);
            return Ok(diaryEntries);
        }

        [HttpPost]
        public ActionResult<DiaryEntryDTO> Create([FromBody] DiaryEntryDTO diaryEntry)
        {
            try
            {
                var retDiaryEntry = _diaryEntryService.CreateDiaryEntry(diaryEntry);
                _logger.LogInformation("Diary entry created. DiaryEntryId={DiaryEntryId} DiaryId={DiaryId}", retDiaryEntry.DiaryEntryId, retDiaryEntry.DiaryId);
                return Created("URI", retDiaryEntry);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database dependency failure creating diary entry. DiaryEntryId={DiaryEntryId} DiaryId={DiaryId}", diaryEntry.DiaryEntryId, diaryEntry.DiaryId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failure creating diary entry. DiaryEntryId={DiaryEntryId} DiaryId={DiaryId}", diaryEntry.DiaryEntryId, diaryEntry.DiaryId);
                throw;
            }
        }

        [HttpPut]
        public ActionResult<DiaryEntryDTO> Update([FromBody] DiaryEntryDTO diaryEntry)
        {
            try
            {
                var retDiaryEntry = _diaryEntryService.UpdateDiaryEntry(diaryEntry);
                _logger.LogInformation("Diary entry updated. DiaryEntryId={DiaryEntryId} DiaryId={DiaryId}", retDiaryEntry.DiaryEntryId, retDiaryEntry.DiaryId);
                return Ok(retDiaryEntry);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database dependency failure updating diary entry. DiaryEntryId={DiaryEntryId} DiaryId={DiaryId}", diaryEntry.DiaryEntryId, diaryEntry.DiaryId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failure updating diary entry. DiaryEntryId={DiaryEntryId} DiaryId={DiaryId}", diaryEntry.DiaryEntryId, diaryEntry.DiaryId);
                throw;
            }
        }

        [Route("{diaryEntryId:guid}")]
        [HttpDelete]
        public ActionResult Delete(Guid diaryEntryId)
        {
            var diaryEntry = _diaryEntryService.GetDiaryEntry(diaryEntryId);
            if (diaryEntry == null)
            {
                return NotFound();
            }

            try
            {
                _diaryEntryService.DeleteDiaryEntry(diaryEntry);
                _logger.LogInformation("Diary entry deleted. DiaryEntryId={DiaryEntryId}", diaryEntryId);
                return Ok();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database dependency failure deleting diary entry. DiaryEntryId={DiaryEntryId}", diaryEntryId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failure deleting diary entry. DiaryEntryId={DiaryEntryId}", diaryEntryId);
                throw;
            }
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<DateTime> GetMinDate(Guid diaryId)
        {
            var date = _diaryEntryService.MinDiaryEntryDate(diaryId);
            return Ok(date);
        }

        [Route("{diaryId:guid}")]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<DateTime> GetMaxDate(Guid diaryId)
        {
            var date = _diaryEntryService.MaxDiaryEntryDate(diaryId);
            return Ok(date);
        }
    }
}
