// <copyright file="SharedTestFactory.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    [TestClass]
    public class SharedTestFactory
    {
        public static readonly JsonSerializerOptions ApiJsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) },
            PropertyNameCaseInsensitive = true,
        };

        public static CustomWebApplicationFactory<Program> Factory { get; private set; } = null!;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            Factory = new CustomWebApplicationFactory<Program>();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Factory.Dispose();
        }
    }
}
