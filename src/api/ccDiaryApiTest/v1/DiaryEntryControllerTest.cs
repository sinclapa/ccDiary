// <copyright file="DiaryEntryControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using System.Collections.Generic;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
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
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object);

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
            var controller = new DiaryEntryController(diaryEntryServiceMock.Object);

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
            diaryEntryServiceMock.Setup(h => h.SearchDiaryEntries(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchType>()))
                .Callback<Guid, DateTime, DateTime, SearchType>((d, f, t, s) =>
                {
                    diaryId = d;
                    from = f;
                    to = t;
                    searchType = s;
                })
                .Returns([2022, 2023]);

            var controller = new DiaryEntryController(diaryEntryServiceMock.Object);

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
            diaryEntryServiceMock.Setup(h => h.SearchDiaryEntries(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchType>()))
                .Callback<Guid, DateTime, DateTime, SearchType>((d, f, t, s) =>
                {
                    diaryId = d;
                    from = f;
                    to = t;
                    searchType = s;
                })
                .Returns([04, 05, 08]);

            var controller = new DiaryEntryController(diaryEntryServiceMock.Object);

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
            diaryEntryServiceMock.Setup(h => h.SearchDiaryEntries(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchType>()))
                .Callback<Guid, DateTime, DateTime, SearchType>((d, f, t, s) =>
                {
                    diaryId = d;
                    from = f;
                    to = t;
                    searchType = s;
                })
                .Returns([7, 13, 23, 30]);

            var controller = new DiaryEntryController(diaryEntryServiceMock.Object);

            // Act
            var id = Guid.NewGuid();
            var response = controller.Search(id, 2022, 5);

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

            var controller = new DiaryEntryController(diaryEntryServiceMock.Object);

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
    }
}
