using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

/// <summary>
/// T020 (US1) — drives <see cref="CSharpExpressionClassifier.Classify(string?)"/> (T029).
/// Asserts that the new expression forms are classified, and that conventional markup
/// extensions (<c>{Binding}</c>, <c>{StaticResource}</c>, <c>{x:Bind}</c>,
/// <c>{TemplateBinding}</c>, <c>{ThemeResource}</c>) are NOT reclassified — they must
/// fall through to the existing pipeline (FR-001, FR-016, contracts/expression-grammar.md).
/// </summary>
/// <remarks>
/// Phase 2 only handles the unambiguous opt-in directives. Phase 3 (T029) extends
/// <see cref="CSharpExpressionClassifier.Classify"/> to recognise bare-identifier,
/// dotted-path, compound, interpolation, method-call, and conditional forms.
/// </remarks>
[TestClass]
public class Given_ExpressionClassifier
{
	[TestMethod]
	public void When_ExplicitDirective_IsClassifiedAsExplicit()
	{
		CSharpExpressionClassifier.Classify("{= Foo}").Should().Be(ExpressionKind.Explicit);
	}

	[TestMethod]
	public void When_ForcedThis_IsClassifiedAsForcedThis()
	{
		CSharpExpressionClassifier.Classify("{this.WindowTitle}").Should().Be(ExpressionKind.ForcedThis);
	}

	[TestMethod]
	public void When_ForcedDataType_IsClassifiedAsForcedDataType()
	{
		CSharpExpressionClassifier.Classify("{.FirstName}").Should().Be(ExpressionKind.ForcedDataType);
	}

	[TestMethod]
	public void When_BareIdentifier_IsClassifiedAsSimpleIdentifier()
	{
		CSharpExpressionClassifier.Classify("{Foo}").Should().Be(ExpressionKind.SimpleIdentifier);
	}

	[TestMethod]
	public void When_DottedPath_IsClassifiedAsDottedPath()
	{
		CSharpExpressionClassifier.Classify("{User.Address.City}").Should().Be(ExpressionKind.DottedPath);
	}

	[TestMethod]
	public void When_BinaryOperator_IsClassifiedAsCompound()
	{
		CSharpExpressionClassifier.Classify("{Price * Quantity}").Should().Be(ExpressionKind.Compound);
	}

	[TestMethod]
	public void When_ConditionalExpression_IsClassifiedAsCompound()
	{
		CSharpExpressionClassifier.Classify("{IsVip ? 'Gold' : 'Standard'}").Should().Be(ExpressionKind.Compound);
	}

	[TestMethod]
	public void When_InterpolatedString_IsClassifiedAsCompound()
	{
		CSharpExpressionClassifier.Classify("{$'{Balance:C2}'}").Should().Be(ExpressionKind.Compound);
	}

	[TestMethod]
	public void When_MethodCall_IsClassifiedAsCompound()
	{
		CSharpExpressionClassifier.Classify("{Math.Max(A, B)}").Should().Be(ExpressionKind.Compound);
	}

	[TestMethod]
	[DataRow("{Binding Foo}")]
	[DataRow("{StaticResource MyBrush}")]
	[DataRow("{x:Bind Foo}")]
	[DataRow("{TemplateBinding Background}")]
	[DataRow("{ThemeResource SystemAccentColor}")]
	[DataRow("{local:FooExtension Bar=1}")]
	public void When_ConventionalMarkupExtension_FallsThrough(string raw)
	{
		// Existing markup extensions MUST NOT be reclassified — backwards compatibility.
		CSharpExpressionClassifier.Classify(raw).Should().BeNull();
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("   ")]
	[DataRow("Hello, world!")]
	[DataRow("plain text")]
	public void When_NotBraceWrapped_FallsThrough(string? raw)
	{
		CSharpExpressionClassifier.Classify(raw).Should().BeNull();
	}

	[TestMethod]
	public void When_EmptyBraces_FallsThrough()
	{
		// `{}` is the XAML literal-escape and must be preserved (per expression-grammar.md
		// §Rejected forms note). Classifier must not treat it as an empty expression.
		CSharpExpressionClassifier.Classify("{}").Should().BeNull();
	}
}
