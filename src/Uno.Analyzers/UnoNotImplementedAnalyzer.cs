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

					if (HasNotImplementedAttribute(notImplementedSymbol, namedSymbol))
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

		private static bool HasNotImplementedAttribute(INamedTypeSymbol notImplementedSymbol, ISymbol namedSymbol)
		{
			return namedSymbol.GetAttributes().Any(a => a.AttributeClass == notImplementedSymbol);
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
				if (HasNotImplementedAttribute(notImplementedSymbol, member.Symbol) || HasNotImplementedAttribute(notImplementedSymbol, member.Symbol.ContainingSymbol))
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
