#nullable enable

using System;
using System.Linq;

namespace Uno.UI.Samples.Tests;

internal record TestCaseResult
{
	public TestResult TestResult { get; init; }

	/// <summary>
	/// Add_WithParameters(1, 2, 3)
	/// </summary>
	public string? TestName { get; init; }

	/// <summary>
	/// MyApp.Tests.Services.CalculatorTests.Add_WithParameters(1,2,3)
	/// </summary>
	public string? FullName => ClassName is not null ? $"{ClassName}.{TestName}" : TestName;

	/// <summary>
	/// Add_WithParameters
	/// </summary>
	public string? MethodName { get; init; }

	/// <summary>
	/// MyApp.Tests.Services.CalculatorTests
	/// </summary>
	public string? ClassName { get; init; }

	public TimeSpan Duration { get; init; }

	public string? Message { get; init; }

	public string? ConsoleOutput { get; init; }
}
