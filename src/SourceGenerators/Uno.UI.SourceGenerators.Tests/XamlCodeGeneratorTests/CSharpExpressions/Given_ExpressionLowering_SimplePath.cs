using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

/// <summary>
/// T023 (US1) — drives <see cref="ExpressionLowering.Lower"/> (T036) for simple-path shapes.
/// Covers <c>contracts/generated-binding-shapes.md</c> §1 (settable two-way), §2 (read-only one-way),
/// and §11 (<c>{.FirstName}</c> forced-datatype one-way).
/// </summary>
[TestClass]
public class Given_ExpressionLowering_SimplePath
{
	[TestMethod]
	public void When_SimpleIdentifierWithPublicSetter_EmitsTwoWaySimplePath()
	{
		var scope = CreateScope(dataTypeMembers: "public string FirstName { get; set; }");
		var analysis = Analyze("FirstName", scope);
		var attr = Classified("{FirstName}", "FirstName", ExpressionKind.SimpleIdentifier);

		var result = ExpressionLowering.Lower(attr, analysis, scope, helperIndex: 0);

		var binding = result.Should().BeOfType<SimplePathBinding>().Subject;
		binding.Path.Should().Be("FirstName");
		binding.Mode.Should().Be(SimplePathBindingMode.TwoWay);
		binding.DataContextSource.Should().Be(DataContextSource.DataType);
	}

	[TestMethod]
	public void When_SimpleIdentifierReadOnly_EmitsOneWaySimplePath()
	{
		var scope = CreateScope(dataTypeMembers: "public string FullName { get; } = \"\";");
		var analysis = Analyze("FullName", scope);
		var attr = Classified("{FullName}", "FullName", ExpressionKind.SimpleIdentifier);

		var result = ExpressionLowering.Lower(attr, analysis, scope, helperIndex: 0);

		var binding = result.Should().BeOfType<SimplePathBinding>().Subject;
		binding.Path.Should().Be("FullName");
		binding.Mode.Should().Be(SimplePathBindingMode.OneWay);
	}

	[TestMethod]
	public void When_DottedPath_PreservesFullPathInSimplePath()
	{
		var scope = CreateScope(
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
		var analysis = Analyze("User.Address.City", scope);
		var attr = Classified("{User.Address.City}", "User.Address.City", ExpressionKind.DottedPath);

		var result = ExpressionLowering.Lower(attr, analysis, scope, helperIndex: 0);

		var binding = result.Should().BeOfType<SimplePathBinding>().Subject;
		binding.Path.Should().Be("User.Address.City");
		binding.Mode.Should().Be(SimplePathBindingMode.TwoWay);
	}

	[TestMethod]
	public void When_ForcedDataType_AlwaysEmitsOneWay()
	{
		var scope = CreateScope(dataTypeMembers: "public string FirstName { get; set; }");
		var analysis = Analyze("FirstName", scope);
		var attr = Classified("{.FirstName}", "FirstName", ExpressionKind.ForcedDataType);

		var result = ExpressionLowering.Lower(attr, analysis, scope, helperIndex: 0);

		var binding = result.Should().BeOfType<SimplePathBinding>().Subject;
		binding.Path.Should().Be("FirstName");
		binding.Mode.Should().Be(SimplePathBindingMode.OneWay);
	}

	[TestMethod]
	public void When_TwoWayTargetButLeafNotSettable_DowngradesToOneWay()
	{
		var scope = CreateScope(dataTypeMembers: "public string FullName { get; } = \"\";");
		var analysis = Analyze("FullName", scope);
		var attr = Classified("{FullName}", "FullName", ExpressionKind.SimpleIdentifier);

		var result = ExpressionLowering.Lower(
			attr,
			analysis,
			scope,
			helperIndex: 0,
			forceOneWayForTwoWayTarget: true);

		result.Should().BeOfType<SimplePathBinding>()
			.Which.Mode.Should().Be(SimplePathBindingMode.OneWay);
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
		string dataTypeMembers,
		string? extraTypes = null)
	{
		var source = $$"""
			namespace TestRepro
			{
				public class TestPage { }

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
