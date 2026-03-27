// <copyright file="AssemblyVersionInfoTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Reflection;
    using System.Reflection.Emit;
    using ccDiaryApi.Utilities;

    [TestClass]
    public class AssemblyVersionInfoTest
    {
        [TestMethod]
        public void GetInformationalVersion_ReturnsVersion_ForCurrentAssembly()
        {
            // Act
            var version = AssemblyVersionInfo.GetInformationalVersion();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(version));
        }

        [TestMethod]
        public void GetInformationalVersion_ReturnsVersion_WhenAssemblyPassedExplicitly()
        {
            // Arrange
            var assembly = Assembly.GetExecutingAssembly();

            // Act
            var version = AssemblyVersionInfo.GetInformationalVersion(assembly);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(version));
        }

        [TestMethod]
        public void GetInformationalVersion_FallsBackToAssemblyVersion_WhenNoInformationalVersion()
        {
            // Arrange — a dynamic assembly has no AssemblyInformationalVersionAttribute
            // and an empty Location, so it falls through to the AssemblyName.Version fallback
            var dynAssembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("DynTestAssembly") { Version = new Version(2, 3, 4, 5) },
                AssemblyBuilderAccess.Run);

            // Act
            var version = AssemblyVersionInfo.GetInformationalVersion(dynAssembly);

            // Assert — should return the version string or "unknown"
            Assert.IsFalse(string.IsNullOrEmpty(version));
            Assert.IsTrue(version == "2.3.4.5" || version == "unknown");
        }

        [TestMethod]
        public void GetInformationalVersion_FallsBackToVersionString_WhenNoInformationalVersionAndNoExplicitVersion()
        {
            // Arrange — dynamic assembly with no InformationalVersion and no explicit Version.
            // In .NET, AssemblyName.Version defaults to 0.0.0.0 (not null), so this returns "0.0.0.0".
            var dynAssembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("DynNoVersionAssembly"),
                AssemblyBuilderAccess.Run);

            // Act
            var version = AssemblyVersionInfo.GetInformationalVersion(dynAssembly);

            // Assert — 0.0.0.0 is the default when no version is explicitly set
            Assert.AreEqual("0.0.0.0", version);
        }
    }
}
