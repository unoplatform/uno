using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing.Verifiers;

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
}
