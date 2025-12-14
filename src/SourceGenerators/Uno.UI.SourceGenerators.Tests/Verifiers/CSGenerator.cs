using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing.Model;
using System.Reflection;

namespace Uno.UI.SourceGenerators.Tests.Verifiers
{
	public static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
		where TSourceGenerator : ISourceGenerator, new()
	{
#pragma warning disable CS0618 // Type or member is obsolete
		public class Test : CSharpSourceGeneratorTest<TSourceGenerator>
#pragma warning restore CS0618 // Type or member is obsolete
		{
			public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;
			public bool IgnoreAccessibility { get; set; }

			protected override CompilationOptions CreateCompilationOptions()
			{
				var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();

				// Hacky way to get the generated code from tests to be able to access internal Uno APIs
				if (IgnoreAccessibility)
				{
					compilationOptions = compilationOptions.WithMetadataImportOptions(MetadataImportOptions.All);
					var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
					topLevelBinderFlagsProperty!.SetValue(compilationOptions, (uint)1 << 22 /*IgnoreAccessibility*/);
				}

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
			protected override IEnumerable<Type> GetSourceGenerators()
			{
				yield return typeof(TSourceGenerator);
			}
		}
	}
}
