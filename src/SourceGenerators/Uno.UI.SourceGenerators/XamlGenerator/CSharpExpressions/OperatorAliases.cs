#nullable enable

using System;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Whitespace-bounded, case-insensitive, string-literal-aware replacement of
/// <c>AND → &amp;&amp;</c>, <c>OR → ||</c>, <c>LT → &lt;</c>, <c>LTE → &lt;=</c>,
/// <c>GT → &gt;</c>, <c>GTE → &gt;=</c>.
/// See <c>contracts/expression-grammar.md</c> §Operator-aliases.
/// </summary>
/// <remarks>
/// Replacement rules:
/// <list type="number">
/// <item>Applied outside of string literals only (single-quoted and interpolated strings are skipped token-by-token).</item>
/// <item>Each alias must be surrounded by whitespace or expression boundary (<c>CountGT0</c> is not a match for <c>GT</c>).</item>
/// <item><c>LTE</c>/<c>GTE</c> are replaced before <c>LT</c>/<c>GT</c> so the longer form wins.</item>
/// </list>
/// </remarks>
internal static class OperatorAliases
{
	// TODO (T085): implement the full replacement. Phase 3 scaffold only — Replace currently returns
	// the input unchanged so the pipeline compiles end-to-end. Drive implementation via the failing
	// Given_OperatorAliases tests (T080).
	public static string Replace(string expression) => expression;
}
