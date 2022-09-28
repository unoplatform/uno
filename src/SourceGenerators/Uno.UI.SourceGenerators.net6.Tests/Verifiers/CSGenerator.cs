using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Uno.UI.SourceGenerators.Tests.Verifiers
{
	public static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
		where TSourceGenerator : ISourceGenerator, new()
	{
		public class Test : CSharpSourceGeneratorTest<TSourceGenerator, MSTestVerifier>
		{
			public Test()
			{
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
		public class Test : SourceGeneratorTest<MSTestVerifier>
		{
			public Test()
			{
			}

			protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
				=> new ISourceGenerator[] { new TSourceGenerator().AsSourceGenerator() };

			protected override string DefaultFileExt => "cs";

			public override string Language => LanguageNames.CSharp;

			protected override GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators)
			{
				return CSharpGeneratorDriver.Create(
					sourceGenerators,
					project.AnalyzerOptions.AdditionalFiles,
					(CSharpParseOptions)project.ParseOptions!,
					project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
			}

			public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

			protected override CompilationOptions CreateCompilationOptions()
			{
				var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
				return compilationOptions.WithSpecificDiagnosticOptions(
					compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
			}

			protected override ParseOptions CreateParseOptions()
			{
				return new CSharpParseOptions(LanguageVersion, DocumentationMode.Diagnose);
			}
		}
	}
}
