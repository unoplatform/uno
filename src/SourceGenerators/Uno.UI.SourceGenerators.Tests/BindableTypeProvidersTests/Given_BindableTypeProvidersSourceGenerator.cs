using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.BindableTypeProviders;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.BindableTypeProvidersTests;

[TestClass]
public class Given_BindableTypeProvidersSourceGenerator
{
	// The provider switches from a switch/case lookup to a dictionary at 1000 non-generic bindable types.
	private const int DictionaryModeTypeCount = 1000;

	private const string GlobalConfig = """
		is_global = true
		build_property.MSBuildProjectFullPath = //Project/0/Project.csproj
		build_property.RootNamespace = MyProject
		build_property.AssemblyName = MyProject
		build_property.IntermediateOutputPath = obj/
		build_property.IsUnoHead = true
		""";

	[TestMethod]
	[DataRow(1, DisplayName = "Switch mode")]
	[DataRow(DictionaryModeTypeCount, DisplayName = "Dictionary mode")]
	public async Task When_KnownMissingTypes_Disabled_In_Debug(int typeCount)
	{
		// The generated provider must compile once _knownMissingTypes is compiled out.
		await CreateTest(typeCount, "DEBUG", "UNO_DISABLE_KNOWN_MISSING_TYPES").RunAsync();
	}

	[TestMethod]
	[DataRow(1, DisplayName = "Switch mode")]
	[DataRow(DictionaryModeTypeCount, DisplayName = "Dictionary mode")]
	public async Task When_KnownMissingTypes_Reported_In_Debug(int typeCount)
	{
		var test = CreateTest(typeCount, "DEBUG");

		await test.RunAsync();

		test.GeneratedCode.Should().Contain("_knownMissingTypes");
		test.GeneratedCode.Should().Contain("""$"The Bindable attribute is missing and the type [{type.FullName}] is not known by the MetadataProvider.""");
	}

	private static Test CreateTest(int typeCount, params string[] preprocessorSymbols)
	{
		var test = new Test(preprocessorSymbols, isDictionaryMode: typeCount >= DictionaryModeTypeCount)
		{
			ReferenceAssemblies = _Dotnet.Current.ReferenceAssemblies,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { GenerateBindableTypes(typeCount) },
				AnalyzerConfigFiles = { ("/.globalconfig", GlobalConfig) },
			},
		};

		test.TestState.AdditionalReferences.AddRange(UnoAssemblyHelper.LoadAssemblies());

		return test;
	}

	private static string GenerateBindableTypes(int count)
	{
		var builder = new StringBuilder("namespace MyProject;\r\n");
		for (var i = 0; i < count; i++)
		{
			builder.Append($"[Microsoft.UI.Xaml.Data.Bindable] public class BindableType{i:0000} {{ public int Value {{ get; set; }} }}\r\n");
		}

		return builder.ToString();
	}

	private sealed class Test(string[] preprocessorSymbols, bool isDictionaryMode) : CSharpSourceGeneratorVerifier<BindableTypeProvidersSourceGenerator>.Test
	{
		public string GeneratedCode { get; private set; } = "";

		protected override ParseOptions CreateParseOptions()
			=> ((CSharpParseOptions)base.CreateParseOptions()).WithPreprocessorSymbols(preprocessorSymbols);

		protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
		{
			var result = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);

			GeneratedCode = result.compilation.SyntaxTrees
				.Single(tree => tree.FilePath.EndsWith("BindableMetadata.g.cs", StringComparison.Ordinal))
				.ToString();

			// Make sure the lookup mode under test is the one generated.
			GeneratedCode.Should().Contain(isDictionaryMode ? "_bindableTypeCacheByFullName" : "_bindableTypes[");

			return result;
		}
	}
}
