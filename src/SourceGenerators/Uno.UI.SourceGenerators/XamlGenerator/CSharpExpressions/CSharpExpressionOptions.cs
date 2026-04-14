#nullable enable

using System;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Reader for the opt-in MSBuild property gating the feature.
/// See <c>contracts/msbuild-properties.md</c>.
/// </summary>
internal static class CSharpExpressionOptions
{
	private const string PropertyName = "UnoXamlCSharpExpressionsEnabled";

	public static bool IsEnabled(GeneratorExecutionContext context) =>
		string.Equals(
			context.GetMSBuildPropertyValue(PropertyName),
			"true",
			StringComparison.OrdinalIgnoreCase);
}
