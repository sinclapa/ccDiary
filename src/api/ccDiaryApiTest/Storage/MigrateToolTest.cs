// <copyright file="MigrateToolTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Storage
{
    using ccDiary.Migrate;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;

    /// <summary>
    /// Tests for the migration tool.
    /// </summary>
    /// <remarks>
    /// The verifier tests matter most. It is the acceptance gate for moving real data,
    /// and a gate that reports success regardless is worse than no gate at all — it
    /// converts "we do not know" into "we checked", which is how a silent data loss ships.
    /// So these assert that it *detects* damage, not merely that it passes on good data.
    /// </remarks>
    [TestClass]
    public class MigrateToolTest
    {
        private StorageTestFixture _fixture = null!;
        private StorageWriter _storage = null!;

        [TestInitialize]
        public async Task Init()
        {
            _fixture = await StorageTestFixture.CreateAsync();
            _storage = new StorageWriter(_fixture.Options);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _fixture?.Dispose();
        }

        // ── Verifier: passes on faithful data ──────────────────────────────────────
        [TestMethod]
        public async Task Verifier_PassesWhenStorageMatchesTheSource()
        {
            var (diary, entries) = await MigrateSampleAsync();

            var verifier = new Verifier(_storage);
            var ok = await verifier.VerifyDiaryAsync(diary, entries);

            Assert.IsTrue(ok, string.Join("; ", verifier.Problems));
            Assert.AreEqual(0, verifier.Problems.Count);
        }

        [TestMethod]
        public async Task Verifier_PassesForImagesRoundTrippedThroughBlobs()
        {
            var (diary, entries) = await MigrateSampleAsync(withImage: true);

            var verifier = new Verifier(_storage);

            Assert.IsTrue(await verifier.VerifyDiaryAsync(diary, entries), string.Join("; ", verifier.Problems));
        }

        // ── Verifier: detects damage ───────────────────────────────────────────────
        [TestMethod]
        public async Task Verifier_DetectsAMissingEntry()
        {
            var (diary, entries) = await MigrateSampleAsync();

            // An entry the source has but storage never received.
            entries.Add(new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = diary.DiaryId!.Value,
                Date = new DateTime(1917, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                Location = "Never written",
                Entry = "Never written",
            });

            var verifier = new Verifier(_storage);
            var ok = await verifier.VerifyDiaryAsync(diary, entries);

            Assert.IsFalse(ok);
            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("missing from storage", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task Verifier_DetectsAlteredText()
        {
            var (diary, entries) = await MigrateSampleAsync();

            entries[0].Entry = "something the migration never wrote";

            var verifier = new Verifier(_storage);

            Assert.IsFalse(await verifier.VerifyDiaryAsync(diary, entries));
            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("entry differs", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task Verifier_DetectsATruncatedImage()
        {
            // The reason images are compared by hash and not by presence or length: a
            // truncated base64 string is still a plausible-looking image.
            var (diary, entries) = await MigrateSampleAsync(withImage: true);

            var withImage = entries.First(e => !string.IsNullOrEmpty(e.ImageData));
            withImage.ImageData = withImage.ImageData![.. (withImage.ImageData!.Length / 2)];

            var verifier = new Verifier(_storage);

            Assert.IsFalse(await verifier.VerifyDiaryAsync(diary, entries));
            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("image content differs", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task Verifier_DetectsAnEntryInStorageThatTheSourceDoesNotHave()
        {
            var (diary, entries) = await MigrateSampleAsync();

            // Something wrote an entry the migration never put there.
            await _storage.WriteEntryAsync(new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = diary.DiaryId!.Value,
                Date = new DateTime(1919, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Location = "Unexpected",
                Entry = "Not from the source",
            });

            var verifier = new Verifier(_storage);

            Assert.IsFalse(await verifier.VerifyDiaryAsync(diary, entries));
            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("in storage but not in the source", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task Verifier_DetectsACompensatingPair_WhereCountsStillMatch()
        {
            // The case a count comparison cannot see: one row missing and one extra, so
            // the totals agree while the contents do not. This is the whole reason the
            // check is by identity rather than by number.
            var (diary, entries) = await MigrateSampleAsync();

            await _storage.WriteEntryAsync(new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = diary.DiaryId!.Value,
                Date = new DateTime(1919, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Location = "Unexpected",
                Entry = "Not from the source",
            });

            // Source expects one the migration never wrote, balancing the count exactly.
            entries.Add(new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = diary.DiaryId!.Value,
                Date = new DateTime(1920, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Location = "Never written",
                Entry = "Never written",
            });

            var actual = await _storage.Entries.GetDiaryEntriesAsync(diary.DiaryId!.Value);
            Assert.AreEqual(entries.Count, actual.Count, "the counts must match, or this is not testing what it claims");

            var verifier = new Verifier(_storage);

            Assert.IsFalse(await verifier.VerifyDiaryAsync(diary, entries));
            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("missing from storage", StringComparison.Ordinal)));
            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("in storage but not in the source", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task Verifier_DetectsAUserInStorageThatTheSourceDoesNotHave()
        {
            // The alarming direction: an account, with a role, that the migration did not
            // create.
            await _storage.WriteUserAsync(new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = "unexpected-admin",
                DisplayName = "Unexpected",
                Email = "unexpected@test.com",
                Role = AppRole.DiaryAdmin,
                CreatedAt = DateTime.UtcNow,
            });

            var verifier = new Verifier(_storage);
            await verifier.VerifyUsersAndRequestsAsync([],[]);

            Assert.IsTrue(verifier.Problems.Any(p =>
                p.Contains("unexpected-admin", StringComparison.Ordinal)
                && p.Contains("in storage but not in the source", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task Verifier_DetectsAnAccessRequestInStorageThatTheSourceDoesNotHave()
        {
            await _storage.WriteAccessRequestAsync(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Unexpected",
                Email = "unexpected@test.com",
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            });

            var verifier = new Verifier(_storage);
            await verifier.VerifyUsersAndRequestsAsync([],[]);

            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("in storage but not in the source", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task Verifier_DetectsADowngradedAdministratorRole()
        {
            // Losing a role silently turns an administrator into a reader, which nobody
            // notices until they try to do something.
            await _storage.WriteUserAsync(new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = "admin-oid",
                DisplayName = "Admin",
                Email = "admin@test.com",
                Role = AppRole.DiaryContributor,
                CreatedAt = DateTime.UtcNow,
            });

            var expected = new List<AppUserDto>
            {
                new ()
                {
                    UserId = Guid.NewGuid(),
                    EntraObjectId = "admin-oid",
                    DisplayName = "Admin",
                    Email = "admin@test.com",
                    Role = AppRole.DiaryAdmin,
                    CreatedAt = DateTime.UtcNow,
                },
            };

            var verifier = new Verifier(_storage);
            await verifier.VerifyUsersAndRequestsAsync(expected,[]);

            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("role", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public async Task Verifier_DetectsAMissingAccessRequest()
        {
            var expected = new List<AccessRequestDto>
            {
                new ()
                {
                    AccessRequestId = Guid.NewGuid(),
                    DisplayName = "Never migrated",
                    Email = "missing@test.com",
                    Status = RequestStatus.Approved,
                    RequestedAt = DateTime.UtcNow,
                },
            };

            var verifier = new Verifier(_storage);
            await verifier.VerifyUsersAndRequestsAsync([], expected);

            Assert.IsTrue(verifier.Problems.Any(p => p.Contains("access request", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task Verifier_DetectsAMissingDiary()
        {
            var verifier = new Verifier(_storage);
            var ok = await verifier.VerifyDiaryAsync(
                new DiaryDTO { DiaryId = Guid.NewGuid(), Title = "Ghost", Author = "Nobody" },
                []);

            Assert.IsFalse(ok);
        }

        [TestMethod]
        public async Task Verifier_TreatsNullAndEmptyStringsAsEquivalent()
        {
            // The relational columns were nullable and the serializer omits nulls, so a
            // "" arriving back as null is expected and must not be reported as damage.
            var diary = new DiaryDTO
            {
                DiaryId = Guid.NewGuid(),
                Title = "Nullable",
                Author = "Author",
                Description = string.Empty,
            };
            await _storage.WriteDiaryAsync(diary);

            var verifier = new Verifier(_storage);

            Assert.IsTrue(await verifier.VerifyDiaryAsync(diary,[]), string.Join("; ", verifier.Problems));
        }

        // ── Writer ────────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task Writer_IsIdempotent_SoAnInterruptedRunIsRepairedByRerunning()
        {
            var (diary, entries) = await MigrateSampleAsync();

            foreach (var entry in entries)
            {
                await _storage.WriteEntryAsync(entry);
            }

            var stored = await _storage.Entries.GetDiaryEntriesAsync(diary.DiaryId!.Value);

            Assert.AreEqual(entries.Count, stored.Count, "re-running duplicated entries");
        }

        [TestMethod]
        public async Task Writer_KeepsAnEntryThatHasNoDate()
        {
            // The API refuses to create one, but a legacy row can hold null and dropping
            // it silently during a migration would be worse than carrying it forward.
            var diaryId = Guid.NewGuid();
            await _storage.WriteDiaryAsync(new DiaryDTO { DiaryId = diaryId, Title = "Dateless", Author = "A" });

            await _storage.WriteEntryAsync(new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = diaryId,
                Date = null,
                Location = "L",
                Entry = "no date on this one",
            });

            var stored = await _storage.Entries.GetDiaryEntriesAsync(diaryId);
            Assert.AreEqual(1, stored.Count);
        }

        [TestMethod]
        public async Task Writer_EnsureCreatedIsSafeToRepeat()
        {
            await _storage.EnsureCreatedAsync();
            await _storage.EnsureCreatedAsync();
        }

        // ── Command line ──────────────────────────────────────────────────────────
        [TestMethod]
        public void CommandLine_RequiresADestination()
        {
            Assert.IsNull(CommandLineOptions.Parse(["--source", "Server=x"]));
        }

        [TestMethod]
        public void CommandLine_RequiresASourceOrAnArchive()
        {
            Assert.IsNull(CommandLineOptions.Parse(["--dest", "someaccount"]));
        }

        [TestMethod]
        public void CommandLine_RejectsUnknownArguments()
        {
            Assert.IsNull(CommandLineOptions.Parse(["--dest", "a", "--source", "b", "--wat"]));
        }

        [TestMethod]
        public void CommandLine_RejectsAMissingArchiveFile()
        {
            Assert.IsNull(CommandLineOptions.Parse(["--dest", "a", "--from-archive", "does-not-exist.json"]));
        }

        [TestMethod]
        public void CommandLine_TreatsADestinationWithoutEqualsAsAnAccountName()
        {
            // An account name means managed identity or the developer's credential; a
            // connection string means Azurite. Getting this backwards would send a
            // migration at the wrong place entirely.
            var options = CommandLineOptions.Parse(["--source", "Server=x", "--dest", "mystorageaccount"]);

            Assert.IsNotNull(options);
            Assert.AreEqual("mystorageaccount", options.ToStorageOptions().AccountName);
            Assert.IsNull(options.ToStorageOptions().ConnectionString);
        }

        [TestMethod]
        public void CommandLine_TreatsADestinationContainingEqualsAsAConnectionString()
        {
            var options = CommandLineOptions.Parse(
                ["--source", "Server=x", "--dest", "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1"]);

            Assert.IsNotNull(options);
            Assert.IsNull(options.ToStorageOptions().AccountName);
            StringAssert.Contains(options.ToStorageOptions().ConnectionString, "devstoreaccount1");
        }

        [TestMethod]
        public void CommandLine_ParsesTheFlags()
        {
            var options = CommandLineOptions.Parse(["--source", "Server=x", "--dest", "acc", "--dry-run", "--verify"]);

            Assert.IsNotNull(options);
            Assert.IsTrue(options.DryRun);
            Assert.IsTrue(options.Verify);
        }

        [TestMethod]
        public void CommandLine_DefaultsBothFlagsOff()
        {
            var options = CommandLineOptions.Parse(["--source", "Server=x", "--dest", "acc"]);

            Assert.IsNotNull(options);
            Assert.IsFalse(options.DryRun);
            Assert.IsFalse(options.Verify);
        }

        [TestMethod]
        public void CommandLine_PrintUsageDoesNotThrow()
        {
            CommandLineOptions.PrintUsage();
        }

        /// <summary>Writes a small diary through the tool and returns what was written.</summary>
        private async Task<(DiaryDTO Diary, List<DiaryEntryDTO> Entries)> MigrateSampleAsync(bool withImage = false)
        {
            var diaryId = Guid.NewGuid();
            var diary = new DiaryDTO
            {
                DiaryId = diaryId,
                Title = "Migrated Diary",
                Author = "Author",
                Description = "Moved from SQL",
                OwnerId = "owner-oid",
            };

            var entries = new List<DiaryEntryDTO>
            {
                new ()
                {
                    DiaryEntryId = Guid.NewGuid(),
                    DiaryId = diaryId,
                    Date = new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc),
                    Location = "Somme",
                    Entry = "First day.",
                    ShowMap = true,
                    MapLocation = "50.0,2.6",
                },
                new ()
                {
                    DiaryEntryId = Guid.NewGuid(),
                    DiaryId = diaryId,
                    Date = new DateTime(1918, 11, 11, 11, 0, 0, DateTimeKind.Utc),
                    Location = "Compiegne",
                    Entry = "Armistice.",
                    ShowJourney = true,
                    FromLocation = "Paris",
                    ToLocation = "Compiegne",
                    JourneyMode = JourneyMode.CrowFlies,
                },
            };

            if (withImage)
            {
                var bytes = new byte[512];
                Random.Shared.NextBytes(bytes);
                entries[0].ImageData = Convert.ToBase64String(bytes);
                entries[0].ImageContentType = "image/png";
            }

            await _storage.WriteDiaryAsync(diary);
            foreach (var entry in entries)
            {
                await _storage.WriteEntryAsync(entry);
            }

            return (diary, entries);
        }
    }
}
