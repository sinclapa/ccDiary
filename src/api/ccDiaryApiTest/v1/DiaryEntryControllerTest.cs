// <copyright file="DiaryEntryControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;

    [TestClass]
    public class DiaryEntryControllerTest
    {
        [TestMethod]
        public void GetValid()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();

            Guid id = Guid.NewGuid();
            var diaryEntry = new DiaryEntryDTO { DiaryEntryId = id, DiaryId = Guid.NewGuid(), Date = DateTime.UtcNow, Location = "London", Entry = "Some text." };
            diaryEntryServiceMock.Setup(x => x.GetDiaryEntry(id)).Returns(diaryEntry);
            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var response = controller.Get(id);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(id, result.DiaryEntryId);
        }

        [TestMethod]
        public void GetInvalid()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var response = controller.Get(Guid.NewGuid());

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(NotFoundResult));
            Assert.IsNull(response.Value);
        }

        [TestMethod]
        public void SearchByDiaryId()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();

            Guid diaryId = Guid.Empty;
            var from = DateTime.UtcNow;
            var to = DateTime.UtcNow;
            var searchType = SearchType.Day;
            diaryEntryServiceMock.Setup(h => h.GetDiaryDateRange(It.IsAny<Guid>()))
                .Returns(new DiaryDateRange { MaxDateTime = DateTime.MaxValue, MinDateTime = DateTime.MinValue });
            diaryEntryServiceMock.Setup(h => h.SearchDiaryEntries(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchType>(), It.IsAny<int>()))
                .Callback<Guid, DateTime, DateTime, SearchType, int>((d, f, t, s, o) =>
                {
                    diaryId = d;
                    from = f;
                    to = t;
                    searchType = s;
                })
                .Returns([2022, 2023]);

            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var id = Guid.NewGuid();
            var response = controller.Search(id);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(DateTime.MinValue, from);
            Assert.AreEqual(DateTime.MaxValue, to);
            Assert.AreEqual(SearchType.Year, searchType);
        }

        [TestMethod]
        public void SearchByYear()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();

            Guid diaryId = Guid.Empty;
            var from = DateTime.UtcNow;
            var to = DateTime.UtcNow;
            var searchType = SearchType.Day;
            diaryEntryServiceMock.Setup(h => h.SearchDiaryEntries(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchType>(), It.IsAny<int>()))
                .Callback<Guid, DateTime, DateTime, SearchType, int>((d, f, t, s, o) =>
                {
                    diaryId = d;
                    from = f;
                    to = t;
                    searchType = s;
                })
                .Returns([04, 05, 08]);

            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var id = Guid.NewGuid();
            var response = controller.Search(id, 2022);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(new DateTime(2022, 1, 1), from);
            Assert.AreEqual(new DateTime(2023, 1, 1).Subtract(new TimeSpan(1)), to);
            Assert.AreEqual(SearchType.Month, searchType);
        }

        [TestMethod]
        public void SearchByYearAndMonth()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();

            Guid diaryId = Guid.Empty;
            var from = DateTime.UtcNow;
            var to = DateTime.UtcNow;
            var searchType = SearchType.Day;
            diaryEntryServiceMock.Setup(h => h.SearchDiaryEntries(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchType>(), It.IsAny<int>()))
                .Callback<Guid, DateTime, DateTime, SearchType, int>((d, f, t, s, o) =>
                {
                    diaryId = d;
                    from = f;
                    to = t;
                    searchType = s;
                })
                .Returns([7, 13, 23, 30]);

            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var id = Guid.NewGuid();
            var response = controller.Search(id, 2022, 5, 0);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(new DateTime(2022, 5, 1), from);
            Assert.AreEqual(new DateTime(2022, 6, 1).Subtract(new TimeSpan(1)), to);
            Assert.AreEqual(SearchType.Day, searchType);
        }

        [TestMethod]
        public void SearchByYearAndMonthWithUTCOffset()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();

            var from = DateTime.UtcNow;
            var to = DateTime.UtcNow;
            var capturedOffset = -1;
            diaryEntryServiceMock.Setup(h => h.SearchDiaryEntries(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchType>(), It.IsAny<int>()))
                .Callback<Guid, DateTime, DateTime, SearchType, int>((d, f, t, s, o) =>
                {
                    from = f;
                    to = t;
                    capturedOffset = o;
                })
                .Returns([7, 13, 23, 24]);

            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act — BST offset (+60 min): local May starts at UTC April 30 23:00
            var id = Guid.NewGuid();
            var response = controller.Search(id, 2022, 5, 60);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            Assert.AreEqual(new DateTime(2022, 4, 30, 23, 0, 0, DateTimeKind.Utc), from);
            Assert.AreEqual(new DateTime(2022, 5, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddMinutes(-60).Subtract(new TimeSpan(1)), to);
            Assert.AreEqual(60, capturedOffset);
        }

        [TestMethod]
        public void SearchByYearAndMonthAndDate()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();

            Guid diaryId = Guid.Empty;
            var from = DateTime.UtcNow;
            var to = DateTime.UtcNow;
            var diaryEntry = new DiaryEntryDTO
            {
                Date = new DateTime(2022, 5, 23, 14, 25, 7, DateTimeKind.Utc),
                DiaryId = Guid.NewGuid(),
                DiaryEntryId = Guid.NewGuid(),
                Entry = "Test entry",
                Location = "Test location",
            };
            diaryEntryServiceMock.Setup(h => h.GetDiaryEntries(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback<Guid, DateTime, DateTime>((d, f, t) =>
                {
                    diaryId = d;
                    from = f;
                    to = t;
                })
                .Returns([diaryEntry]);

            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var id = Guid.NewGuid();
            var response = controller.Search(id, 2022, 5, 23, 0);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new DateTime(2022, 5, 23, 0, 0, 0, DateTimeKind.Utc).ToUniversalTime(), from);
            Assert.AreEqual(new DateTime(2022, 5, 24, 0, 0, 0, DateTimeKind.Utc).Subtract(new TimeSpan(1)).ToUniversalTime(), to);
        }

        [TestMethod]
        public void CreatePassesThroughShowJourneyFields()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();

            DiaryEntryDTO? captured = null;
            var diaryEntry = new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Location = "Sandwich, UK",
                Entry = "Left Sandwich.",
                ShowJourney = true,
                FromLocation = "Sandwich, UK",
                ToLocation = "Southampton, UK",
            };
            diaryEntryServiceMock
                .Setup(x => x.CreateDiaryEntry(It.IsAny<DiaryEntryDTO>()))
                .Callback<DiaryEntryDTO>(d => captured = d)
                .Returns(diaryEntry);

            var diaryServiceMock = new Mock<IDiaryService>();
            diaryServiceMock.Setup(x => x.GetDiary(It.IsAny<Guid>()))
                .Returns(new DiaryDTO { DiaryId = diaryEntry.DiaryId, Title = "Test", Author = "Test", OwnerId = null });
            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var response = controller.Create(diaryEntry);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(CreatedResult));
            Assert.IsNotNull(captured);
            Assert.IsTrue(captured.ShowJourney);
            Assert.AreEqual("Sandwich, UK", captured.FromLocation);
            Assert.AreEqual("Southampton, UK", captured.ToLocation);
        }

        [TestMethod]
        public void UpdatePassesThroughShowJourneyFields()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();

            DiaryEntryDTO? captured = null;
            var diaryEntry = new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Location = "London, UK",
                Entry = "Journey entry.",
                ShowJourney = true,
                FromLocation = "London, UK",
                ToLocation = "Paris, France",
            };
            diaryEntryServiceMock
                .Setup(x => x.UpdateDiaryEntry(It.IsAny<DiaryEntryDTO>()))
                .Callback<DiaryEntryDTO>(d => captured = d)
                .Returns(diaryEntry);

            var diaryServiceMock = new Mock<IDiaryService>();
            diaryServiceMock.Setup(x => x.GetDiary(It.IsAny<Guid>()))
                .Returns(new DiaryDTO { DiaryId = diaryEntry.DiaryId, Title = "Test", Author = "Test", OwnerId = null });
            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var response = controller.Update(diaryEntry);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            Assert.IsNotNull(captured);
            Assert.IsTrue(captured.ShowJourney);
            Assert.AreEqual("London, UK", captured.FromLocation);
            Assert.AreEqual("Paris, France", captured.ToLocation);
        }

        [TestMethod]
        public void Create_AsNonOwner_ReturnsForbid()
        {
            var diaryEntry = new DiaryEntryDTO { DiaryEntryId = Guid.NewGuid(), DiaryId = Guid.NewGuid(), Date = DateTime.UtcNow, Location = "L", Entry = "E" };
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            var diaryServiceMock = new Mock<IDiaryService>();
            diaryServiceMock.Setup(x => x.GetDiary(It.IsAny<Guid>()))
                .Returns(new DiaryDTO { Title = "T", Author = "A", OwnerId = "owner-oid" });

            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object, oid: "other-oid");
            var response = controller.Create(diaryEntry);

            Assert.IsInstanceOfType(response.Result, typeof(ForbidResult));
        }

        [TestMethod]
        public void Create_AsAdmin_ReturnsCreated()
        {
            var diaryEntry = new DiaryEntryDTO { DiaryEntryId = Guid.NewGuid(), DiaryId = Guid.NewGuid(), Date = DateTime.UtcNow, Location = "L", Entry = "E" };
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            diaryEntryServiceMock.Setup(x => x.CreateDiaryEntry(It.IsAny<DiaryEntryDTO>())).Returns(diaryEntry);
            var diaryServiceMock = new Mock<IDiaryService>();

            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object, oid: "admin-oid", isAdmin: true);
            var response = controller.Create(diaryEntry);

            Assert.IsInstanceOfType(response.Result, typeof(CreatedResult));
        }

        [TestMethod]
        public void Update_AsNonOwner_ReturnsForbid()
        {
            var diaryEntry = new DiaryEntryDTO { DiaryEntryId = Guid.NewGuid(), DiaryId = Guid.NewGuid(), Date = DateTime.UtcNow, Location = "L", Entry = "E" };
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            var diaryServiceMock = new Mock<IDiaryService>();
            diaryServiceMock.Setup(x => x.GetDiary(It.IsAny<Guid>()))
                .Returns(new DiaryDTO { Title = "T", Author = "A", OwnerId = "owner-oid" });

            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object, oid: "other-oid");
            var response = controller.Update(diaryEntry);

            Assert.IsInstanceOfType(response.Result, typeof(ForbidResult));
        }

        [TestMethod]
        public void Update_AsAdmin_ReturnsOk()
        {
            var diaryEntry = new DiaryEntryDTO { DiaryEntryId = Guid.NewGuid(), DiaryId = Guid.NewGuid(), Date = DateTime.UtcNow, Location = "L", Entry = "E" };
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            diaryEntryServiceMock.Setup(x => x.UpdateDiaryEntry(It.IsAny<DiaryEntryDTO>())).Returns(diaryEntry);
            var diaryServiceMock = new Mock<IDiaryService>();

            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object, oid: "admin-oid", isAdmin: true);
            var response = controller.Update(diaryEntry);

            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public void Delete_EntryNotFound_ReturnsNotFound()
        {
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            var response = controller.Delete(Guid.NewGuid());

            Assert.IsInstanceOfType(response, typeof(NotFoundResult));
        }

        [TestMethod]
        public void Delete_AsOwner_ReturnsOk()
        {
            var diaryEntry = new DiaryEntryDTO { DiaryEntryId = Guid.NewGuid(), DiaryId = Guid.NewGuid(), Date = DateTime.UtcNow, Location = "L", Entry = "E" };
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            diaryEntryServiceMock.Setup(x => x.GetDiaryEntry(diaryEntry.DiaryEntryId!.Value)).Returns(diaryEntry);
            var diaryServiceMock = new Mock<IDiaryService>();
            diaryServiceMock.Setup(x => x.GetDiary(It.IsAny<Guid>()))
                .Returns(new DiaryDTO { Title = "T", Author = "A", OwnerId = "owner-oid" });

            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object, oid: "owner-oid");
            var response = controller.Delete(diaryEntry.DiaryEntryId!.Value);

            Assert.IsInstanceOfType(response, typeof(OkResult));
        }

        [TestMethod]
        public void Delete_AsNonOwner_ReturnsForbid()
        {
            var diaryEntry = new DiaryEntryDTO { DiaryEntryId = Guid.NewGuid(), DiaryId = Guid.NewGuid(), Date = DateTime.UtcNow, Location = "L", Entry = "E" };
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            diaryEntryServiceMock.Setup(x => x.GetDiaryEntry(diaryEntry.DiaryEntryId!.Value)).Returns(diaryEntry);
            var diaryServiceMock = new Mock<IDiaryService>();
            diaryServiceMock.Setup(x => x.GetDiary(It.IsAny<Guid>()))
                .Returns(new DiaryDTO { Title = "T", Author = "A", OwnerId = "owner-oid" });

            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object, oid: "other-oid");
            var response = controller.Delete(diaryEntry.DiaryEntryId!.Value);

            Assert.IsInstanceOfType(response, typeof(ForbidResult));
        }

        [TestMethod]
        public void Delete_AsAdmin_ReturnsOk()
        {
            var diaryEntry = new DiaryEntryDTO { DiaryEntryId = Guid.NewGuid(), DiaryId = Guid.NewGuid(), Date = DateTime.UtcNow, Location = "L", Entry = "E" };
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            diaryEntryServiceMock.Setup(x => x.GetDiaryEntry(diaryEntry.DiaryEntryId!.Value)).Returns(diaryEntry);
            var diaryServiceMock = new Mock<IDiaryService>();

            var controller = CreateController(diaryEntryServiceMock.Object, diaryServiceMock.Object, oid: "admin-oid", isAdmin: true);
            var response = controller.Delete(diaryEntry.DiaryEntryId!.Value);

            Assert.IsInstanceOfType(response, typeof(OkResult));
        }

        [TestMethod]
        public void TextSearch_ReturnsMatchingEntries()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var matchingEntry = new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = diaryId,
                Date = new DateTime(2020, 6, 1, 9, 0, 0, DateTimeKind.Utc),
                Location = "Ypres",
                Entry = "Arrived at the Menin Gate.",
            };
            var paged = new PagedResultDTO<DiaryEntryDTO>
            {
                Items =[matchingEntry],
                TotalCount = 1,
                Page = 1,
                PageSize = 20,
            };

            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            diaryEntryServiceMock
                .Setup(x => x.TextSearchDiaryEntries(diaryId, "Menin", 1, 20))
                .Returns(paged);
            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var response = controller.TextSearch(diaryId, "Menin");

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual("Ypres", result.Items.First().Location);
        }

        [TestMethod]
        public void TextSearch_MatchesLocation()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var paged = new PagedResultDTO<DiaryEntryDTO>
            {
                Items =[new DiaryEntryDTO { DiaryId = diaryId, Location = "Passchendaele", Entry = "Quiet day.", Date = DateTime.UtcNow }],
                TotalCount = 1,
                Page = 1,
                PageSize = 20,
            };

            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            diaryEntryServiceMock
                .Setup(x => x.TextSearchDiaryEntries(diaryId, "Passchendaele", 1, 20))
                .Returns(paged);
            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var response = controller.TextSearch(diaryId, "Passchendaele");

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.TotalCount);
        }

        [TestMethod]
        public void TextSearch_EmptySearch_ReturnsBadRequest()
        {
            // Arrange
            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var response = controller.TextSearch(Guid.NewGuid(), "   ");

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public void TextSearch_NoMatch_ReturnsEmptyPage()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var emptyPaged = new PagedResultDTO<DiaryEntryDTO>
            {
                Items =[],
                TotalCount = 0,
                Page = 1,
                PageSize = 20,
            };

            var diaryEntryServiceMock = new Mock<IDiaryEntryService>();
            diaryEntryServiceMock
                .Setup(x => x.TextSearchDiaryEntries(diaryId, "zzznomatch", 1, 20))
                .Returns(emptyPaged);
            var diaryServiceMock = new Mock<IDiaryService>();
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object, diaryServiceMock.Object);

            // Act
            var response = controller.TextSearch(diaryId, "zzznomatch");

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalCount);
            Assert.AreEqual(0, result.Items.Count());
        }

        private static DiaryEntryController CreateController(IDiaryEntryService entryService, IDiaryService diaryService, string? oid = null, bool isAdmin = false)
        {
            var claims = new List<Claim>();
            if (oid != null)
            {
                claims.Add(new Claim("oid", oid));
            }

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "DiaryAdmin"));
            }

            var controller = new DiaryEntryController(entryService, diaryService);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Count > 0 ? "Test" : string.Empty)),
                },
            };
            return controller;
        }
    }
}
