using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		internal const string Title = "This API is not available in VS15.8 and ealier";
		internal const string MessageFormat = "{0} is not available in VS15.8 and ealier";
		internal const string Description = "This API is not available in VS15.8 and ealier, choose a different overload";
		internal const string Category = "Compatibility";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			"Uno0002",
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

			context.RegisterCompilationStartAction(csa =>
			{
				var stringSymbol = csa.Compilation.GetTypeByMetadataName("System.String");
				var charSymbol = csa.Compilation.GetTypeByMetadataName("System.Char");

				csa.RegisterSyntaxNodeAction(c => OnMemberAccessExpression(c, stringSymbol, charSymbol), SyntaxKind.SimpleMemberAccessExpression);
			});
		}

		private static bool HasNotImplementedAttribute(INamedTypeSymbol notImplementedSymbol, ISymbol namedSymbol)
		{
			return namedSymbol.GetAttributes().Any(a => a.AttributeClass == notImplementedSymbol);
		}

		private void OnMemberAccessExpression(
			SyntaxNodeAnalysisContext contextAnalysis,
			INamedTypeSymbol stringSymbol,
			INamedTypeSymbol charSymbol
		)
		{
			if (IsBindableMetadata(contextAnalysis))
			{
				return;
			}

			var memberAccess = contextAnalysis.Node as MemberAccessExpressionSyntax;
			var member = contextAnalysis.SemanticModel.GetSymbolInfo(memberAccess);

			if (member.Symbol != null && member.Symbol.ContainingSymbol == stringSymbol)
			{
				var validateMembers = new[] {
					"Split",
					"TrimStart",
					"TrimEnd"
				};

				if(
					validateMembers.Any(m => member.Symbol.Name == m)
					&& member.Symbol is IMethodSymbol method
					&& method.Parameters.FirstOrDefault()?.Type == charSymbol
				)
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
		{
			return Path.GetFileName(contextAnalysis.Node?.GetLocation()?.SourceTree?.FilePath) == "BindableMetadata.g.cs";
		}
	}
}
