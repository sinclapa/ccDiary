using System.ComponentModel.DataAnnotations;

namespace ccDiaryApi.Data.Model
{
    public class DiaryExportDTO
    {
        public required DiaryDTO Diary {  get; set; }
        public required List<DiaryEntryDTO> DiaryEntries { get; set; }
    }
}
