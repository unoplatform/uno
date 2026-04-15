using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests.CSharpExpressions;

/// <summary>
/// T021 (US1) — drives <see cref="MemberResolver.Resolve(string, ResolutionScope)"/> (T034).
/// Covers the bare-identifier rows of the decision table in
/// <c>contracts/resolution-algorithm.md</c> §Resolution that are in US1 scope:
/// <c>This</c>, <c>DataType</c>, <c>Both</c> (UNO2002), <c>Neither</c> (UNO2003),
/// and the markup-extension fallthrough.
/// </summary>
/// <remarks>
/// These tests are RED against the current Phase 2 stub of <see cref="MemberResolver"/>,
/// which always returns <c>(Neither, null, null)</c>. T034 will make them green.
/// US3 (UNO2001) and US4 (UNO2004 / static-type) cases are deferred to T060 / T070.
/// </remarks>
[TestClass]
public class Given_MemberResolver
{
	[TestMethod]
	public void When_MemberOnPageOnly_ResolvesAsThis()
	{
		var scope = CreateScope(
			pageMembers: "public string PageOnly { get; set; }",
			dataTypeMembers: "public string DataOnly { get; set; }");

		var result = MemberResolver.Resolve("PageOnly", scope);

		result.Location.Should().Be(MemberLocation.This);
		result.Symbol.Should().NotBeNull();
		result.Diagnostic.Should().BeNull();
	}

	[TestMethod]
	public void When_MemberOnDataTypeOnly_ResolvesAsDataType()
	{
		var scope = CreateScope(
			pageMembers: "public string PageOnly { get; set; }",
			dataTypeMembers: "public string DataOnly { get; set; }");

		var result = MemberResolver.Resolve("DataOnly", scope);

		result.Location.Should().Be(MemberLocation.DataType);
		result.Symbol.Should().NotBeNull();
		result.Diagnostic.Should().BeNull();
	}

	[TestMethod]
	public void When_MemberOnBothPageAndDataType_EmitsUNO2002()
	{
		var scope = CreateScope(
			pageMembers: "public string Shared { get; set; }",
			dataTypeMembers: "public string Shared { get; set; }");

		var result = MemberResolver.Resolve("Shared", scope);

		result.Location.Should().Be(MemberLocation.Both);
		result.Diagnostic.Should().NotBeNull();
		result.Diagnostic!.Id.Should().Be("UNO2002");
	}

	[TestMethod]
	public void When_MemberMissingEverywhere_EmitsUNO2003()
	{
		var scope = CreateScope(
			pageMembers: "public string PageOnly { get; set; }",
			dataTypeMembers: "public string DataOnly { get; set; }");

		var result = MemberResolver.Resolve("Missing", scope);

		result.Location.Should().Be(MemberLocation.Neither);
		result.Diagnostic.Should().NotBeNull();
		result.Diagnostic!.Id.Should().Be("UNO2003");
	}

	[TestMethod]
	public void When_MemberMissingButMarkupExtensionExists_ResolvesAsMarkupExtension()
	{
		var scope = CreateScope(
			pageMembers: "",
			dataTypeMembers: "",
			extraTypes: "public class FooExtension : Microsoft.UI.Xaml.Markup.MarkupExtension { protected override object ProvideValue() => null!; }",
			markupExtensions: new() { ["Foo"] = "TestRepro.FooExtension" });

		var result = MemberResolver.Resolve("Foo", scope);

		result.Location.Should().Be(MemberLocation.MarkupExtension);
		result.Diagnostic.Should().BeNull();
	}

	[TestMethod]
	public void When_DataTypeIsNull_BareIdentifierResolvesAgainstPageOnly()
	{
		var scope = CreateScope(
			pageMembers: "public string PageOnly { get; set; }",
			dataTypeMembers: null);

		var result = MemberResolver.Resolve("PageOnly", scope);

		result.Location.Should().Be(MemberLocation.This);
		result.Diagnostic.Should().BeNull();
	}

	private static ResolutionScope CreateScope(
		string pageMembers,
		string? dataTypeMembers,
		string? extraTypes = null,
		Dictionary<string, string>? markupExtensions = null)
	{
		var dataTypeBlock = dataTypeMembers is null
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
		var references = new List<MetadataReference> { mscorlib };

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			new[] { tree },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var pageType = compilation.GetTypeByMetadataName("TestRepro.TestPage")!;
		var dataType = dataTypeMembers is null ? null : compilation.GetTypeByMetadataName("TestRepro.TestDataType");

		var knownMarkupExtensions = new Dictionary<string, INamedTypeSymbol>();
		if (markupExtensions is not null)
		{
			foreach (var kvp in markupExtensions)
			{
				var symbol = compilation.GetTypeByMetadataName(kvp.Value);
				if (symbol is not null)
				{
					knownMarkupExtensions[kvp.Key] = symbol;
				}
			}
		}

		return new ResolutionScope(
			pageType,
			dataType,
			knownMarkupExtensions,
			globalUsings: System.Array.Empty<string>(),
			compilation);
	}
}
