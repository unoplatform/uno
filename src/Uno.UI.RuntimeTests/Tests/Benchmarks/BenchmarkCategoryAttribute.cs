#nullable enable

using System;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks;

public enum BenchmarkCategory
{
	Micro,
	Scenario,
	Render,
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class BenchmarkCategoryAttribute : Attribute
{
	public BenchmarkCategoryAttribute(BenchmarkCategory category)
	{
		Category = category;
	}

	public BenchmarkCategory Category { get; }
}
