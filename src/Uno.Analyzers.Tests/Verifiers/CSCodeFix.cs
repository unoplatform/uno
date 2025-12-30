using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;

namespace Uno.Analyzers.Tests.Verifiers
{
	// This class is a copy from https://github.com/dotnet/roslyn-sdk/tree/main/src/VisualStudio.Roslyn.SDK/Roslyn.SDK/ProjectTemplates/CSharp/Diagnostic/Test/Verifiers
	public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
		where TAnalyzer : DiagnosticAnalyzer, new()
		where TCodeFix : CodeFixProvider, new()
	{
		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic()"/>
		public static DiagnosticResult Diagnostic()
			=> CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Diagnostic();

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(string)"/>
		public static DiagnosticResult Diagnostic(string diagnosticId)
			=> CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Diagnostic(diagnosticId);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
		public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
			=> CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Diagnostic(descriptor);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
		public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
		{
			var test = new Test
			{
				TestCode = source,
			};

			test.ExpectedDiagnostics.AddRange(expected);
			await test.RunAsync(CancellationToken.None);
		}

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
		public static async Task VerifyCodeFixAsync(string source, string fixedSource)
			=> await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
		public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
			=> await VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

		/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
		public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
		{
			var test = new Test
			{
				TestCode = source,
				FixedCode = fixedSource,
			};

			test.ExpectedDiagnostics.AddRange(expected);
			await test.RunAsync(CancellationToken.None);
		}
	}

}
