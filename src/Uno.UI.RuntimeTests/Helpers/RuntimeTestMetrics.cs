#nullable enable

using System.Collections.Generic;
using System.Globalization;

namespace Uno.UI.RuntimeTests.Helpers;

/// <summary>
/// Measurements a runtime test reports alongside its result. The runner clears them before each attempt and writes
/// them into the NUnit results as test-case properties (<c>metric:</c>, <c>budget:</c>, <c>unit:</c> and
/// <c>description:</c> prefixes), so
/// CI can collect them from every lane without the test knowing where it runs.
/// </summary>
/// <remarks>
/// Budgets are upper bounds on counts (frames, damaged area, path operations, objects left alive), never timings:
/// timings vary several-fold between CI runs, counts do not. Recording a value over its budget does not fail the
/// test; a test that wants to enforce one asserts on it as well.
/// </remarks>
public static class RuntimeTestMetrics
{
	private static readonly object _gate = new();
	private static List<RuntimeTestMetric> _metrics = new();

	/// <param name="description">What the value counts and what a good value is, in a sentence: it is the legend of
	/// the pull request report.</param>
	public static void Record(string name, double value, double? budget = null, string? unit = null, string? description = null)
	{
		lock (_gate)
		{
			_metrics.Add(new RuntimeTestMetric(name, value, budget, unit, description));
		}
	}

	public static void Reset()
	{
		lock (_gate)
		{
			_metrics = new();
		}
	}

	public static IReadOnlyList<RuntimeTestMetric> TakeAll()
	{
		lock (_gate)
		{
			var taken = _metrics;
			_metrics = new();
			return taken;
		}
	}
}

public sealed record RuntimeTestMetric(string Name, double Value, double? Budget, string? Unit, string? Description = null)
{
	public bool IsOverBudget => Budget is { } budget && Value > budget;

	public override string ToString()
		=> string.Create(CultureInfo.InvariantCulture, $"{Name}={Value:0.###}{Unit}{(Budget is { } b ? $" (budget {b:0.###})" : "")}");
}
