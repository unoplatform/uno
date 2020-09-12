#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnoNotImplementedAnalyzer : DiagnosticAnalyzer
	{
		internal const string Title = "Uno type or member is not implemented";
		internal const string MessageFormat = "{0} is not implemented in Uno";
		internal const string Description = "This member or type is not implemented and will fail when invoked.";
		internal const string Category = "Compatibility";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			"Uno0001",
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: Description
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(csa => {

				var notImplementedSymbol = csa.Compilation.GetTypeByMetadataName("Uno.NotImplementedAttribute");

				csa.RegisterSyntaxNodeAction(c => OnMemberAccessExpression(c, notImplementedSymbol), SyntaxKind.SimpleMemberAccessExpression);
				csa.RegisterSyntaxNodeAction(c => OnObjectCreationExpression(c, notImplementedSymbol), SyntaxKind.ObjectCreationExpression);
			}
			);
		}

		private void OnObjectCreationExpression(SyntaxNodeAnalysisContext contextAnalysis, INamedTypeSymbol notImplementedSymbol)
		{
			if (IsBindableMetadata(contextAnalysis))
			{
				return;
			}

			var objectCreation = contextAnalysis.Node as ObjectCreationExpressionSyntax;

			if (objectCreation != null)
			{
				var symbol = contextAnalysis.SemanticModel.GetSymbolInfo(objectCreation.Type);

				var namedSymbol = symbol.Symbol as INamedTypeSymbol;

				if (namedSymbol != null && IsUnoSymbol(symbol))
				{
					var directives = GetDirectives(contextAnalysis);

					if (HasNotImplementedAttribute(notImplementedSymbol, namedSymbol, directives))
					{
						var diagnostic = Diagnostic.Create(
							SupportedDiagnostics.First(),
							contextAnalysis.Node.GetLocation(),
							symbol.Symbol.ToDisplayString()
						);
						contextAnalysis.ReportDiagnostic(diagnostic);
					}
				}
			}
		}

		private string[] GetDirectives(SyntaxNodeAnalysisContext contextAnalysis)
		{
			var directives = contextAnalysis.Node.GetLocation()?.SourceTree.Options.PreprocessorSymbolNames.ToArray() ?? new string[0];

			if (directives.Length == 0)
			{
				// This case is only used during tests where explicit #define statements are
				// present at the top of the file. In common cases, PreprocessorSymbolNames is
				// not empty.

				var directive = contextAnalysis
					.Node
					.GetLocation()
					?.SourceTree
					.GetRoot()
					.GetFirstDirective() as DefineDirectiveTriviaSyntax;

				if (directive != null)
				{
					directives = new[] { directive.Name.Text };
				}
			}

			return directives;
		}

		private static bool HasNotImplementedAttribute(INamedTypeSymbol notImplementedSymbol, ISymbol namedSymbol, string[] directives)
		{
			if(namedSymbol.GetAttributes().FirstOrDefault(a => Equals(a.AttributeClass, notImplementedSymbol)) is AttributeData data)
			{
				if (
					data.ConstructorArguments.FirstOrDefault() is TypedConstant constant
					&& constant.Kind != TypedConstantKind.Error)
				{
					Debug.Assert(constant.Kind == TypedConstantKind.Array);

					var notImplementedPlatforms = constant.Values.Select(v => v.Value?.ToString()).ToArray();

					if (directives.Contains("UNO_REFERENCE_API")
						&& !directives.Contains("__SKIA__")
						&& !directives.Contains("__WASM__"))
					{
						// Uno reference API is a special case where if a member or symbol
						// is implementer for either __SKIA__ or __WASM__, the member is considered
						// implemented. The code may be running in either environments, and we cannot
						// statically determine if a member will be available.
						return notImplementedPlatforms.Any(p => p  ==  "__SKIA__")
							&& notImplementedPlatforms.Any(p => p == "__WASM__");
					}
					else
					{
						return notImplementedPlatforms.Any(d => directives.Contains(d));
					}
				}
				else
				{
					return true;
				}
			}

			return false;
		}

		private void OnMemberAccessExpression(SyntaxNodeAnalysisContext contextAnalysis, INamedTypeSymbol notImplementedSymbol)
		{
			if (IsBindableMetadata(contextAnalysis))
			{
				return;
			}

			var memberAccess = contextAnalysis.Node as MemberAccessExpressionSyntax;

			var member = contextAnalysis.SemanticModel.GetSymbolInfo(memberAccess);

			if (member.Symbol != null && IsUnoSymbol(member))
			{
				var directives = GetDirectives(contextAnalysis);

				var isMemberNotImplemented = HasNotImplementedAttribute(notImplementedSymbol, member.Symbol, directives);
				var isMemberOwnerNotImplemented = HasNotImplementedAttribute(notImplementedSymbol, member.Symbol.ContainingSymbol, directives);

				if (isMemberNotImplemented || isMemberOwnerNotImplemented)
				{
					var diagnostic = Diagnostic.Create(
						SupportedDiagnostics.First(),
						contextAnalysis.Node.GetLocation(),
						member.Symbol.ToDisplayString()
					);
					contextAnalysis.ReportDiagnostic(diagnostic);
				}
			}
		}

		private static bool IsUnoSymbol(SymbolInfo member)
		{
			string name = member.Symbol?.ContainingAssembly?.Name ?? "";

			return name.StartsWith("Uno", StringComparison.Ordinal) || name.Equals("TestProject", StringComparison.Ordinal);
		}

		private static bool IsBindableMetadata(SyntaxNodeAnalysisContext contextAnalysis)
			=> Path.GetFileName(contextAnalysis.Node?.GetLocation()?.SourceTree?.FilePath) == "BindableMetadata.g.cs";
	}
}
