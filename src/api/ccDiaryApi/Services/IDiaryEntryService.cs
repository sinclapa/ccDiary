using ccDiaryApi.Data.Model;

namespace ccDiaryApi.Services
{
    public interface IDiaryEntryService
    {
        List<int> SearchDiaryEntries(Guid diaryId, DateTime from, DateTime to, SearchType searchType);

        List<DiaryEntryDTO> GetDiaryEntries(Guid diaryId, DateTime from, DateTime to);

        List<DiaryEntryDTO> GetDiaryEntries(Guid diaryId);

        DiaryEntryDTO? GetDiaryEntry(Guid id);

        DiaryDateRange GetDiaryDateRange(Guid diaryId);

        void DeleteDiaryEntry(DiaryEntryDTO diaryEntry);

        DiaryEntryDTO CreateDiaryEntry(DiaryEntryDTO diaryEntry);

        DiaryEntryDTO UpdateDiaryEntry(DiaryEntryDTO diaryEntry);

        DateTime MinDiaryEntryDate(Guid diaryId);

        DateTime MaxDiaryEntryDate(Guid diaryId);
    }
}
