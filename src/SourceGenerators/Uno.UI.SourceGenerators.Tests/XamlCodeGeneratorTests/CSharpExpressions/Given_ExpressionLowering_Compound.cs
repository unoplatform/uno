using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

/// <summary>
/// T024 (US1) — drives <see cref="ExpressionLowering.Lower"/> (T037) for compound shapes.
/// Covers <c>contracts/generated-binding-shapes.md</c> §3 (compound), §4 (captures), §5 (one-shot
/// direct assignment), §6 (interpolation), §7 (ternary + null-coalescing).
/// </summary>
[TestClass]
public class Given_ExpressionLowering_Compound
{
	[TestMethod]
	public void When_SimpleArithmetic_EmitsHelperWithDataTypeParameter()
	{
		var scope = CreateScope(
			dataTypeMembers: """
				public decimal Price { get; set; }
				public int Quantity { get; set; }
				""");
		var analysis = Analyze("Price * Quantity", scope);
		var attr = Classified("{Price * Quantity}", "Price * Quantity", ExpressionKind.Compound);

		var result = ExpressionLowering.Lower(
			attr,
			analysis,
			scope,
			helperIndex: 0,
			targetPropertyTypeFullName: "decimal");

		var compound = result.Should().BeOfType<CompoundBinding>().Subject;
		compound.HelperMethodName.Should().Be("__xcs_Expr_000");
		compound.HelperMethodBody.Should().Be(
			"private decimal __xcs_Expr_000(global::TestRepro.TestDataType __source) => __source.Price * __source.Quantity;");
		compound.Handlers.Select(h => h.PropertyName).Should().BeEquivalentTo(new[] { "Price", "Quantity" });
		compound.Captures.Should().BeEmpty();
	}

	[TestMethod]
	public void When_CompoundWithCapture_EmitsHelperWithExtraCaptureParameter()
	{
		var scope = CreateScope(
			pageMembers: "public decimal TaxRate { get; set; } = 1.21m;",
			dataTypeMembers: "public decimal Price { get; set; }");
		var analysis = Analyze("Price * this.TaxRate", scope);
		var attr = Classified("{Price * this.TaxRate}", "Price * this.TaxRate", ExpressionKind.Compound);

		var result = ExpressionLowering.Lower(
			attr,
			analysis,
			scope,
			helperIndex: 2,
			targetPropertyTypeFullName: "decimal");

		var compound = result.Should().BeOfType<CompoundBinding>().Subject;
		compound.HelperMethodName.Should().Be("__xcs_Expr_002");
		compound.HelperMethodBody.Should().Be(
			"private decimal __xcs_Expr_002(global::TestRepro.TestDataType __source, decimal __capture_TaxRate) => __source.Price * __capture_TaxRate;");
		compound.Handlers.Should().ContainSingle()
			.Which.PropertyName.Should().Be("Price");
		compound.Captures.Should().ContainSingle()
			.Which.CaptureVariableName.Should().Be("__capture_TaxRate");
	}

	[TestMethod]
	public void When_TernaryWithNullCoalesce_EmitsInSingleExpressionHelper()
	{
		var scope = CreateScope(
			dataTypeMembers: """
				public bool IsVip { get; set; }
				public string Nickname { get; set; }
				""");
		var analysis = Analyze("IsVip ? 'Gold' : (Nickname ?? 'Anonymous')", scope);
		var attr = Classified(
			"{IsVip ? 'Gold' : (Nickname ?? 'Anonymous')}",
			"IsVip ? 'Gold' : (Nickname ?? 'Anonymous')",
			ExpressionKind.Compound);

		var result = ExpressionLowering.Lower(
			attr,
			analysis,
			scope,
			helperIndex: 4,
			targetPropertyTypeFullName: "string");

		var compound = result.Should().BeOfType<CompoundBinding>().Subject;
		compound.HelperMethodBody.Should().Contain("__source.IsVip ? \"Gold\" : (__source.Nickname ?? \"Anonymous\")");
		compound.HelperMethodBody.Should().Contain("global::System.Convert.ToString((");
		compound.Handlers.Select(h => h.PropertyName).Should().BeEquivalentTo(new[] { "IsVip", "Nickname" });
	}

	[TestMethod]
	public void When_Interpolation_EmitsInterpolatedStringHelper()
	{
		var scope = CreateScope(
			dataTypeMembers: """
				public decimal Price { get; set; }
				public int Quantity { get; set; }
				""");
		var analysis = Analyze("$'{Price:C2} × {Quantity}'", scope);
		var attr = Classified(
			"{$'{Price:C2} × {Quantity}'}",
			"$'{Price:C2} × {Quantity}'",
			ExpressionKind.Compound);

		var result = ExpressionLowering.Lower(
			attr,
			analysis,
			scope,
			helperIndex: 3,
			targetPropertyTypeFullName: "string");

		var compound = result.Should().BeOfType<CompoundBinding>().Subject;
		compound.HelperMethodBody.Should().Contain("$\"{__source.Price:C2} × {__source.Quantity}\"");
		compound.HelperMethodBody.Should().Contain("global::System.Convert.ToString((");
		compound.Handlers.Select(h => h.PropertyName).Should().BeEquivalentTo(new[] { "Price", "Quantity" });
	}

	[TestMethod]
	public void When_OnlyPageCapture_EmitsDirectAssignment()
	{
		var scope = CreateScope(
			pageMembers: "public string WindowTitle { get; set; }",
			dataTypeMembers: "public string Placeholder { get; set; }");
		var analysis = Analyze("this.WindowTitle", scope);
		var attr = Classified("{this.WindowTitle}", "this.WindowTitle", ExpressionKind.ForcedThis);

		var result = ExpressionLowering.Lower(attr, analysis, scope, helperIndex: 0);

		var direct = result.Should().BeOfType<DirectAssignment>().Subject;
		direct.CSharpExpression.Should().Be("__capture_WindowTitle");
		direct.Captures.Should().ContainSingle()
			.Which.OriginalIdentifier.Should().Be("WindowTitle");
	}

	[TestMethod]
	public void When_HelperIndexFormatted_ThreeDigitPadding()
	{
		ExpressionLowering.BuildHelperName(0).Should().Be("__xcs_Expr_000");
		ExpressionLowering.BuildHelperName(7).Should().Be("__xcs_Expr_007");
		ExpressionLowering.BuildHelperName(42).Should().Be("__xcs_Expr_042");
		ExpressionLowering.BuildHelperName(999).Should().Be("__xcs_Expr_999");
	}

	private static ExpressionAnalysisResult Analyze(string inner, ResolutionScope scope)
	{
		var (tree, _) = CSharpExpressionParser.Parse(inner);
		return ExpressionAnalyzer.Analyze(tree, scope);
	}

	private static XamlExpressionAttributeValue Classified(
		string rawText,
		string innerCSharp,
		ExpressionKind kind)
		=> new(
			RawText: rawText,
			Kind: kind,
			InnerCSharp: innerCSharp,
			SourceSpan: new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, rawText.Length)),
			IsCData: false);

	private static ResolutionScope CreateScope(
		string? pageMembers = null,
		string dataTypeMembers = "",
		string? extraTypes = null)
	{
		var source = $$"""
			namespace TestRepro
			{
				public class TestPage
				{
					{{pageMembers ?? string.Empty}}
				}

				public class TestDataType
				{
					{{dataTypeMembers}}
				}

				{{extraTypes ?? string.Empty}}
			}
			""";

		var tree = CSharpSyntaxTree.ParseText(source);

		MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
		MetadataReference objectModel = MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().Assembly.Location);
		MetadataReference runtime = MetadataReference.CreateFromFile(System.IO.Path.Combine(
			System.IO.Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location)!,
			"System.Runtime.dll"));
		var references = new List<MetadataReference> { mscorlib, objectModel, runtime };

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			new[] { tree },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var pageType = compilation.GetTypeByMetadataName("TestRepro.TestPage")!;
		var dataType = compilation.GetTypeByMetadataName("TestRepro.TestDataType")!;

		return new ResolutionScope(
			pageType,
			dataType,
			new Dictionary<string, INamedTypeSymbol>(),
			globalUsings: System.Array.Empty<string>(),
			compilation);
	}
}
