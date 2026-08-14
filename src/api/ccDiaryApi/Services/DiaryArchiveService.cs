// <copyright file="DiaryArchiveService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    /// <summary>
    /// Whole-diary export and import.
    /// </summary>
    /// <remarks>
    /// Built on the diary and entry services rather than on storage directly, so that
    /// image blobs, JSON spill and row key derivation have exactly one implementation.
    /// <para>
    /// This is no longer atomic. The relational version committed an import in a single
    /// transaction; here it is a sequence of blob writes and row upserts, so a failure
    /// part way through leaves a partial diary. Every write is an upsert keyed by the
    /// archive's own identifiers, so re-running the same import repairs it.
    /// </para>
    /// </remarks>
    public class DiaryArchiveService : IDiaryArchiveService
    {
        private readonly IDiaryService _diaryService;
        private readonly IDiaryEntryService _diaryEntryService;

        /// <summary>Initializes a new instance of the <see cref="DiaryArchiveService"/> class.</summary>
        /// <param name="diaryService">The diary service.</param>
        /// <param name="diaryEntryService">The diary entry service.</param>
        public DiaryArchiveService(IDiaryService diaryService, IDiaryEntryService diaryEntryService)
        {
            _diaryService = diaryService;
            _diaryEntryService = diaryEntryService;
        }

        /// <inheritdoc/>
        public async Task<DiaryArchiveDTO?> ExportAsync(Guid diaryId)
        {
            var diary = await _diaryService.GetDiaryAsync(diaryId);
            if (diary == null)
            {
                return null;
            }

            var entries = await _diaryEntryService.GetDiaryEntriesAsync(diaryId);
            return new DiaryArchiveDTO { Diary = diary, DiaryEntries = entries };
        }

        /// <inheritdoc/>
        public async Task<DiaryDTO> ImportAsync(DiaryArchiveDTO diaryArchive)
        {
            diaryArchive.Diary.DiaryId ??= Guid.NewGuid();
            await _diaryService.UpdateAsync(diaryArchive.Diary);

            foreach (var entry in diaryArchive.DiaryEntries)
            {
                entry.DiaryEntryId ??= Guid.NewGuid();
                if (entry.DiaryId == Guid.Empty)
                {
                    entry.DiaryId = diaryArchive.Diary.DiaryId!.Value;
                }

                await _diaryEntryService.UpdateDiaryEntryAsync(entry);
            }

            return diaryArchive.Diary;
        }
    }
}
