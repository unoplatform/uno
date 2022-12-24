using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Testing;

namespace Uno.UI.SourceGenerators.Tests.Verifiers
{
	public static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
		where TSourceGenerator : ISourceGenerator, new()
	{
		public class Test : CSharpSourceGeneratorTest<TSourceGenerator, MSTestVerifier>
		{
			public Test()
			{
				TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
					is_global = true
					build_property.Configuration = Release
					"""));
			}
			public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

			protected override CompilationOptions CreateCompilationOptions()
			{
				var compilationOptions = base.CreateCompilationOptions();
				return compilationOptions.WithSpecificDiagnosticOptions(
					compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
			}

			protected override ParseOptions CreateParseOptions()
			{
				return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
			}
		}
	}

	public static partial class CSharpIncrementalSourceGeneratorVerifier<TSourceGenerator>
		where TSourceGenerator : IIncrementalGenerator, new()
	{
		public class Test : CSharpSourceGeneratorVerifier<EmptySourceGeneratorProvider>.Test
		{
			protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
				=> new ISourceGenerator[] { new TSourceGenerator().AsSourceGenerator() };
		}
	}
}
