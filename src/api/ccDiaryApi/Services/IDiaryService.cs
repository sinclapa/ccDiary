using ccDiaryApi.Data.Model;

namespace ccDiaryApi.Services
{
    public interface IDiaryService
    {
        IEnumerable<DiaryDTO> Get();

        DiaryDTO? Get(Guid diaryId);

        DiaryDTO Create(DiaryDTO diary);

        DiaryDTO Update(DiaryDTO diary);

        void Delete(DiaryDTO diary);
    }
}
