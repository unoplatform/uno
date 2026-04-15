using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

/// <summary>
/// T022 (US1) — drives <see cref="ExpressionAnalyzer.Analyze(SyntaxTree, ResolutionScope)"/> (T035).
/// Covers refresh-set computation, capture/source separation, and two-way inference per
/// <c>contracts/resolution-algorithm.md</c> §Analysis and §Refresh-set (FR-013).
/// </summary>
[TestClass]
public class Given_ExpressionAnalyzer
{
	[TestMethod]
	public void When_SingleDataTypeIdentifier_EmitsOneHandler()
	{
		var scope = CreateScope(
			pageMembers: "",
			dataTypeMembers: "public string FirstName { get; set; }");

		var result = Analyze("FirstName", scope);

		result.Handlers.Should().HaveCount(1);
		result.Handlers[0].Accessor.Should().Be("__source => __source");
		result.Handlers[0].PropertyName.Should().Be("FirstName");
		result.TransformedCSharp.Should().Contain("__source.FirstName");
		result.Captures.Should().BeEmpty();
		result.IsOneShot.Should().BeFalse();
		result.IsSettable.Should().BeTrue();
	}

	[TestMethod]
	public void When_DottedDataTypePath_EmitsHandlerPerHop()
	{
		var scope = CreateScope(
			pageMembers: "",
			dataTypeMembers: "public UserDto User { get; set; }",
			extraTypes: """
				public class UserDto : System.ComponentModel.INotifyPropertyChanged
				{
					public AddressDto Address { get; set; }
					public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
				}
				public class AddressDto : System.ComponentModel.INotifyPropertyChanged
				{
					public string City { get; set; }
					public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
				}
				""");

		var result = Analyze("User.Address.City", scope);

		result.Handlers.Should().HaveCount(3);
		result.Handlers[0].Accessor.Should().Be("__source => __source");
		result.Handlers[0].PropertyName.Should().Be("User");
		result.Handlers[1].Accessor.Should().Be("__source => __source.User");
		result.Handlers[1].PropertyName.Should().Be("Address");
		result.Handlers[2].Accessor.Should().Be("__source => __source.User?.Address");
		result.Handlers[2].PropertyName.Should().Be("City");
		result.TransformedCSharp.Should().Contain("__source.User.Address.City");
		result.Captures.Should().BeEmpty();
		result.IsSettable.Should().BeTrue();
		result.IsOneShot.Should().BeFalse();
	}

	[TestMethod]
	public void When_CompoundDataTypeOperands_EmitsTwoHandlers()
	{
		var scope = CreateScope(
			pageMembers: "",
			dataTypeMembers: """
				public decimal Price { get; set; }
				public int Quantity { get; set; }
				""");

		var result = Analyze("Price * Quantity", scope);

		result.Handlers.Should().HaveCount(2);
		result.Handlers.Select(h => h.PropertyName).Should().BeEquivalentTo(new[] { "Price", "Quantity" });
		result.Handlers.Should().OnlyContain(h => h.Accessor == "__source => __source");
		result.TransformedCSharp.Should().Contain("__source.Price");
		result.TransformedCSharp.Should().Contain("__source.Quantity");
		result.IsSettable.Should().BeFalse();
		result.IsOneShot.Should().BeFalse();
	}

	[TestMethod]
	public void When_MixedDataTypeAndPageCapture_SeparatesHandlersFromCaptures()
	{
		var scope = CreateScope(
			pageMembers: "public decimal TaxRate { get; set; } = 1.21m;",
			dataTypeMembers: "public decimal Price { get; set; }");

		var result = Analyze("Price * this.TaxRate", scope);

		result.Handlers.Should().HaveCount(1);
		result.Handlers[0].PropertyName.Should().Be("Price");
		result.Handlers[0].Accessor.Should().Be("__source => __source");
		result.Captures.Should().HaveCount(1);
		result.Captures[0].OriginalIdentifier.Should().Be("TaxRate");
		result.Captures[0].CaptureVariableName.Should().Be("__capture_TaxRate");
		result.TransformedCSharp.Should().Contain("__source.Price");
		result.TransformedCSharp.Should().Contain("__capture_TaxRate");
		result.TransformedCSharp.Should().NotContain("this.TaxRate");
		result.IsOneShot.Should().BeFalse();
		result.IsSettable.Should().BeFalse();
	}

	[TestMethod]
	public void When_OnlyPageCaptures_IsOneShot()
	{
		var scope = CreateScope(
			pageMembers: "public string WindowTitle { get; set; }",
			dataTypeMembers: "");

		var result = Analyze("this.WindowTitle", scope);

		result.Handlers.Should().BeEmpty();
		result.Captures.Should().HaveCount(1);
		result.Captures[0].CaptureVariableName.Should().Be("__capture_WindowTitle");
		result.IsOneShot.Should().BeTrue();
		result.IsSettable.Should().BeFalse();
	}

	[TestMethod]
	public void When_LeafHasNoSetter_IsNotSettable()
	{
		var scope = CreateScope(
			pageMembers: "",
			dataTypeMembers: "public string FullName { get; } = \"\";");

		var result = Analyze("FullName", scope);

		result.Handlers.Should().HaveCount(1);
		result.IsSettable.Should().BeFalse();
	}

	private static ExpressionAnalysisResult Analyze(string inner, ResolutionScope scope)
	{
		var (tree, _) = CSharpExpressionParser.Parse(inner);
		return ExpressionAnalyzer.Analyze(tree, scope);
	}

	private static ResolutionScope CreateScope(
		string pageMembers,
		string dataTypeMembers,
		string? extraTypes = null)
	{
		var dataTypeBlock = string.IsNullOrEmpty(dataTypeMembers)
			? string.Empty
			: $$"""
				public class TestDataType
				{
					{{dataTypeMembers}}
				}
			""";

		var source = $$"""
			namespace TestRepro
			{
				public class TestPage
				{
					{{pageMembers}}
				}

				{{dataTypeBlock}}

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
		var dataType = string.IsNullOrEmpty(dataTypeMembers)
			? null
			: compilation.GetTypeByMetadataName("TestRepro.TestDataType");

		return new ResolutionScope(
			pageType,
			dataType,
			new Dictionary<string, INamedTypeSymbol>(),
			globalUsings: System.Array.Empty<string>(),
			compilation);
	}
}
