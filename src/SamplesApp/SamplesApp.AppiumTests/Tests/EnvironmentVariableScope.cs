#nullable enable

using System;
using System.Collections.Generic;

namespace SamplesApp.AppiumTests.Tests;

internal sealed class EnvironmentVariableScope : IDisposable
{
	private readonly Dictionary<string, string?> _originalValues = new(StringComparer.Ordinal);

	public void Set(string variableName, string? value)
	{
		if (!_originalValues.ContainsKey(variableName))
		{
			_originalValues[variableName] = Environment.GetEnvironmentVariable(variableName);
		}

		Environment.SetEnvironmentVariable(variableName, value);
	}

	public void Dispose()
	{
		foreach (var pair in _originalValues)
		{
			Environment.SetEnvironmentVariable(pair.Key, pair.Value);
		}
	}
}
