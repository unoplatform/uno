using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Uno.Analyzers.Tests.Verifiers
{
	// This class is a copy from https://github.com/dotnet/roslyn-sdk/tree/main/src/VisualStudio.Roslyn.SDK/Roslyn.SDK/ProjectTemplates/CSharp/Diagnostic/Test/Verifiers
	public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
		where TAnalyzer : DiagnosticAnalyzer, new()
		where TCodeFix : CodeFixProvider, new()
	{
		public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, MSTestVerifier>
		{
			public Test()
			{
				SolutionTransforms.Add((solution, projectId) =>
				{
					var compilationOptions = solution.GetProject(projectId).CompilationOptions;
					compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
						compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
					solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

					return solution;
				});
			}
		}
	}

}
