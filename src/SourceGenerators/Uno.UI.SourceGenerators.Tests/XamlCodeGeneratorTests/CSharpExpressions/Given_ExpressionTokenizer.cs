using System.Linq;
using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

using TokenKind = CSharpExpressionTokenizer.TokenKind;

/// <summary>
/// T018 (US1) — drives <see cref="CSharpExpressionTokenizer"/> implementation (T030).
/// Covers the token boundaries the tokenizer must recognise so downstream alias
/// replacement and quote transformation never mutate string-literal content
/// (FR-002, FR-006, contracts/expression-grammar.md §Parsing-order).
/// </summary>
[TestClass]
public class Given_ExpressionTokenizer
{
	[TestMethod]
	public void When_Empty_ReturnsNoTokens()
	{
		CSharpExpressionTokenizer.Tokenize("").Should().BeEmpty();
	}

	[TestMethod]
	public void When_SingleQuotedString_EmitsStringLiteralToken()
	{
		var tokens = CSharpExpressionTokenizer.Tokenize("'hello'");

		tokens.Should().ContainSingle(t => t.Kind == TokenKind.StringLiteral);
		var literal = tokens.Single(t => t.Kind == TokenKind.StringLiteral);
		literal.Start.Should().Be(0);
		literal.Length.Should().Be(7);
	}

	[TestMethod]
	public void When_SingleQuotedString_WithEscapedQuote_PreservesAsSingleLiteral()
	{
		// `'it\'s'` — the embedded escaped single quote must NOT prematurely close
		// the literal; otherwise alias/quote rewrites would corrupt user text.
		var tokens = CSharpExpressionTokenizer.Tokenize(@"'it\'s'");

		tokens.Where(t => t.Kind == TokenKind.StringLiteral).Should().ContainSingle();
		var literal = tokens.Single(t => t.Kind == TokenKind.StringLiteral);
		literal.Length.Should().Be(7);
	}

	[TestMethod]
	public void When_SingleQuotedString_ContainsDoubleQuote_KeepsLiteralIntact()
	{
		// Double quotes inside single-quoted strings are content, not delimiters.
		var tokens = CSharpExpressionTokenizer.Tokenize("'a\"b'");

		tokens.Where(t => t.Kind == TokenKind.StringLiteral).Should().ContainSingle();
	}

	[TestMethod]
	public void When_InterpolatedString_EmitsInterpolationBoundaryTokens()
	{
		var tokens = CSharpExpressionTokenizer.Tokenize("$'{Name}: {Count}'");

		tokens.Should().Contain(t => t.Kind == TokenKind.InterpolatedStringStart);
		tokens.Should().Contain(t => t.Kind == TokenKind.InterpolationExpressionStart);
		tokens.Should().Contain(t => t.Kind == TokenKind.InterpolationExpressionEnd);
		tokens.Should().Contain(t => t.Kind == TokenKind.InterpolatedStringEnd);

		// At least one identifier reference inside the interpolation hole.
		tokens.Should().Contain(t => t.Kind == TokenKind.Identifier);
	}

	[TestMethod]
	public void When_BinaryExpression_EmitsIdentifiersAndOperator()
	{
		var tokens = CSharpExpressionTokenizer.Tokenize("Foo + Bar");

		tokens.Where(t => t.Kind == TokenKind.Identifier).Should().HaveCount(2);
		tokens.Should().Contain(t => t.Kind == TokenKind.Operator);
	}

	[TestMethod]
	public void When_AliasCandidate_InsideStringLiteral_IsNotTokenizedAsOperator()
	{
		// 'AND' inside a string literal must remain inside the StringLiteral span;
		// it is the OperatorAliases pass that must skip these regions.
		var tokens = CSharpExpressionTokenizer.Tokenize("'AND'");

		tokens.Should().ContainSingle(t => t.Kind == TokenKind.StringLiteral);
		tokens.Should().NotContain(t => t.Kind == TokenKind.Identifier && t.Length == 3);
	}
}
