using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

/// <summary>
/// T080 (US5) — drives <see cref="OperatorAliases.Replace(string)"/> (implementation
/// landed early under T085). Asserts the whitespace-bounded, case-insensitive,
/// string-literal-aware replacement of XML-friendly operator aliases
/// (<c>AND</c>, <c>OR</c>, <c>LT</c>, <c>LTE</c>, <c>GT</c>, <c>GTE</c>) per
/// FR-004 and contracts/expression-grammar.md §Operator-aliases.
/// </summary>
[TestClass]
public class Given_OperatorAliases
{
	[TestMethod]
	[DataRow("Count GT 0", "Count > 0")]
	[DataRow("Count LT 10", "Count < 10")]
	[DataRow("Count GTE 0", "Count >= 0")]
	[DataRow("Count LTE 10", "Count <= 10")]
	[DataRow("IsEnabled AND IsVisible", "IsEnabled && IsVisible")]
	[DataRow("IsEnabled OR IsBusy", "IsEnabled || IsBusy")]
	public void When_Alias_IsReplaced(string input, string expected)
	{
		OperatorAliases.Replace(input).Should().Be(expected);
	}

	[TestMethod]
	[DataRow("Count gt 0", "Count > 0")]
	[DataRow("Count Gt 0", "Count > 0")]
	[DataRow("a and b", "a && b")]
	[DataRow("a Or b", "a || b")]
	public void When_AliasCaseVaries_StillReplaced(string input, string expected)
	{
		OperatorAliases.Replace(input).Should().Be(expected);
	}

	[TestMethod]
	[DataRow("CountGT0", "CountGT0")]
	[DataRow("LTE", "<=")]
	[DataRow("MyANDValue", "MyANDValue")]
	[DataRow("AndOr", "AndOr")]
	[DataRow("orderId", "orderId")]
	public void When_AliasIsPartOfIdentifier_NotReplaced(string input, string expected)
	{
		// Aliases must be whole identifier words — a substring match must not fire.
		OperatorAliases.Replace(input).Should().Be(expected);
	}

	[TestMethod]
	public void When_LTE_IsPresent_LongerFormWins()
	{
		// LTE/GTE must be replaced before LT/GT so the result is `<=` not `<E`.
		OperatorAliases.Replace("a LTE b").Should().Be("a <= b");
		OperatorAliases.Replace("a GTE b").Should().Be("a >= b");
	}

	[TestMethod]
	[DataRow("'a AND b'", "'a AND b'")]
	[DataRow("'count GT 0'", "'count GT 0'")]
	[DataRow("'it\\'s AND it'", "'it\\'s AND it'")]
	public void When_AliasInsideSingleQuotedString_NotReplaced(string input, string expected)
	{
		OperatorAliases.Replace(input).Should().Be(expected);
	}

	[TestMethod]
	public void When_AliasInsideInterpolatedStringLiteralPart_NotReplaced()
	{
		// Inside `$'... AND ...'`, the literal text outside `{ }` must not be touched.
		OperatorAliases.Replace("$'value AND text'").Should().Be("$'value AND text'");
	}

	[TestMethod]
	public void When_AliasInsideInterpolationHole_IsReplaced()
	{
		// Inside the interpolation hole `{ ... }`, expression tokens *are* replaced.
		OperatorAliases.Replace("$'{a GT b}'").Should().Be("$'{a > b}'");
	}

	[TestMethod]
	public void When_MixedAliasesAndOperators_AllReplaced()
	{
		OperatorAliases.Replace("a GT 0 AND b LT 10").Should().Be("a > 0 && b < 10");
	}

	[TestMethod]
	[DataRow("", "")]
	[DataRow("Foo", "Foo")]
	[DataRow("a + b", "a + b")]
	public void When_NoAlias_ReturnsInputUnchanged(string input, string expected)
	{
		OperatorAliases.Replace(input).Should().Be(expected);
	}

	[TestMethod]
	public void When_Null_ReturnsNull()
	{
		OperatorAliases.Replace(null!).Should().BeNull();
	}
}
