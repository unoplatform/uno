using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

/// <summary>
/// T019 (US1) — drives <see cref="CSharpExpressionParser"/> implementation (T032).
/// Asserts the Roslyn AST shape produced for each accepted form in
/// contracts/expression-grammar.md (property access, dotted path, ternary,
/// null-coalescing, arithmetic, interpolation).
/// </summary>
/// <remarks>
/// The parser must apply <c>OperatorAliases.Replace</c> and <c>QuoteTransform.Transform</c>
/// before handing text to Roslyn, otherwise single-quoted strings and alias forms parse
/// as errors. T032 wires those passes; until then the alias and single-quote tests fail.
/// </remarks>
[TestClass]
public class Given_ExpressionParser
{
	[TestMethod]
	public void When_SimpleIdentifier_ParsesAsIdentifierName()
	{
		var (tree, hasErrors) = CSharpExpressionParser.Parse("Foo");

		hasErrors.Should().BeFalse();
		var expr = GetExpression(tree);
		expr.Should().BeOfType<IdentifierNameSyntax>()
			.Which.Identifier.ValueText.Should().Be("Foo");
	}

	[TestMethod]
	public void When_DottedPath_ParsesAsMemberAccessChain()
	{
		var (tree, hasErrors) = CSharpExpressionParser.Parse("User.Address.City");

		hasErrors.Should().BeFalse();
		var expr = GetExpression(tree);
		expr.Should().BeOfType<MemberAccessExpressionSyntax>()
			.Which.Name.Identifier.ValueText.Should().Be("City");
	}

	[TestMethod]
	public void When_Ternary_ParsesAsConditionalExpression()
	{
		var (tree, hasErrors) = CSharpExpressionParser.Parse("IsVip ? 'Gold' : 'Standard'");

		hasErrors.Should().BeFalse();
		GetExpression(tree).Should().BeOfType<ConditionalExpressionSyntax>();
	}

	[TestMethod]
	public void When_NullCoalescing_ParsesAsCoalesceExpression()
	{
		var (tree, hasErrors) = CSharpExpressionParser.Parse("Nickname ?? 'Anonymous'");

		hasErrors.Should().BeFalse();
		var expr = GetExpression(tree);
		expr.Should().BeAssignableTo<BinaryExpressionSyntax>()
			.Which.Kind().Should().Be(SyntaxKind.CoalesceExpression);
	}

	[TestMethod]
	public void When_Arithmetic_ParsesAsBinaryExpression()
	{
		var (tree, hasErrors) = CSharpExpressionParser.Parse("Price * Quantity");

		hasErrors.Should().BeFalse();
		var expr = GetExpression(tree);
		expr.Should().BeAssignableTo<BinaryExpressionSyntax>()
			.Which.Kind().Should().Be(SyntaxKind.MultiplyExpression);
	}

	[TestMethod]
	public void When_InterpolatedString_ParsesAsInterpolatedStringExpression()
	{
		// Single-quoted interpolated string must be rewritten to double-quoted before parsing.
		var (tree, hasErrors) = CSharpExpressionParser.Parse("$'{Balance:C2}'");

		hasErrors.Should().BeFalse();
		GetExpression(tree).Should().BeOfType<InterpolatedStringExpressionSyntax>();
	}

	[TestMethod]
	public void When_OperatorAlias_IsReplacedBeforeParse()
	{
		// `AND` must become `&&` so Roslyn sees a valid logical-and expression.
		var (tree, hasErrors) = CSharpExpressionParser.Parse("Count GT 0 AND IsEnabled");

		hasErrors.Should().BeFalse();
		GetExpression(tree).Should().BeAssignableTo<BinaryExpressionSyntax>()
			.Which.Kind().Should().Be(SyntaxKind.LogicalAndExpression);
	}

	[TestMethod]
	public void When_MultiStatementBody_ReportsParseError()
	{
		// Multi-statement is rejected by FR/UNO2006; Roslyn surfaces a parse diagnostic
		// the analyzer later promotes to UNO2006.
		var (_, hasErrors) = CSharpExpressionParser.Parse("A; B");

		hasErrors.Should().BeTrue();
	}

	private static ExpressionSyntax GetExpression(SyntaxTree tree)
	{
		var root = tree.GetRoot();
		// Script-mode top-level: GlobalStatement → ExpressionStatement → expression.
		var exprStatement = root.DescendantNodes().OfType<ExpressionStatementSyntax>().FirstOrDefault();
		if (exprStatement is not null)
		{
			return exprStatement.Expression;
		}

		return root.DescendantNodes().OfType<ExpressionSyntax>().First();
	}
}
