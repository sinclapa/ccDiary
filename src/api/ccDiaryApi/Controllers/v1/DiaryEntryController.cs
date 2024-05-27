using Asp.Versioning;
using ccDiaryApi.Data.Model;
using ccDiaryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ccDiaryApi.Controllers.v1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    public class DiaryEntryController : ControllerBase
    {
        private readonly IDiaryEntryService _diaryEntryService;

        public DiaryEntryController(IDiaryEntryService diaryEntryService)
        {
            _diaryEntryService = diaryEntryService;
        }

        [Route("{diaryId:guid}")]
        [HttpGet()]
        public ActionResult<List<int>> Search(Guid diaryId)
        {
            var range = _diaryEntryService.GetDiaryDateRange(diaryId);
            var years = _diaryEntryService.SearchDiaryEntries(diaryId, range.minDateTime, range.maxDateTime, SearchType.Year);
            return Ok(years);
        }

        [Route("{diaryId:guid}/{year:int}")]
        [HttpGet()]
        public ActionResult<List<int>> Search(Guid diaryId, int year)
        {
            DateTime from = new DateTime(year, 1, 1);
            DateTime to = from.AddYears(1).Subtract(new TimeSpan(1));
            var months = _diaryEntryService.SearchDiaryEntries(diaryId, from, to, SearchType.Month);
            return Ok(months);
        }

        [Route("{diaryId:guid}/{year:int}/{month:int}")]
        [HttpGet()]
        public ActionResult<List<int>> Search(Guid diaryId, int year, int month)
        {
            DateTime from = new DateTime(year, month, 1);
            DateTime to = from.AddMonths(1).Subtract(new TimeSpan(1));
            var days = _diaryEntryService.SearchDiaryEntries(diaryId, from, to, SearchType.Day);
            return Ok(days);
        }

        [Route("{diaryId:guid}/{year:int}/{month:int}/{day:int}")]
        [HttpGet()]
        public ActionResult<List<DiaryEntryDTO>> Search(Guid diaryId, int year, int month, int day)
        {
            DateTime from = new DateTime(year, month, day);
            DateTime to = from.AddDays(1).Subtract(new TimeSpan(1));
            var searchResult = _diaryEntryService.GetDiaryEntries(diaryId, from, to);
            return Ok(searchResult);
        }

        [Route("{diaryEntryId:guid}")]
        [HttpGet()]
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
        [HttpGet()]
        public ActionResult<IEnumerable<DiaryEntryDTO>> GetDiaryEntries(Guid diaryId)
        {
            var diaryEntries = _diaryEntryService.GetDiaryEntries(diaryId);
            return Ok(diaryEntries);
        }

        [HttpPost()]
        public ActionResult<DiaryEntryDTO> Create([FromBody] DiaryEntryDTO diaryEntry)
        {
            var retDiaryEntry = _diaryEntryService.CreateDiaryEntry(diaryEntry);
            return Created("URI", retDiaryEntry);
        }

        [HttpPut()]
        public ActionResult<DiaryEntryDTO> Update([FromBody] DiaryEntryDTO diaryEntry)
        {
            var retDiaryEntry = _diaryEntryService.UpdateDiaryEntry(diaryEntry);
            return Ok(retDiaryEntry);
        }

        [Route("{diaryEntryId:guid}")]
        [HttpDelete()]
        public ActionResult Delete(Guid diaryEntryId)
        {
            var diaryEntry = _diaryEntryService.GetDiaryEntry(diaryEntryId);
            if (diaryEntry == null)
                return NotFound();
            _diaryEntryService.DeleteDiaryEntry(diaryEntry);
            return Ok();
        }

        [Route("{diaryId:guid}")]
        [HttpGet()]
        public ActionResult<DateTime> GetMinDate(Guid diaryId)
        {
            var date = _diaryEntryService.MinDiaryEntryDate(diaryId);
            return Ok(date);
        }

        [Route("{diaryId:guid}")]
        [HttpGet()]
        public ActionResult<DateTime> GetMaxDate(Guid diaryId)
        {
            var date = _diaryEntryService.MaxDiaryEntryDate(diaryId);
            return Ok(date);
        }
    }
}
