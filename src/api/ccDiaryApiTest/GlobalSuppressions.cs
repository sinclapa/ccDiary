// <copyright file="GlobalSuppressions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

// Test methods intentionally use the MethodName_Scenario_ExpectedResult underscore convention.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "Test methods follow the MethodName_Scenario_ExpectedResult naming convention.",
    Scope = "module")]
