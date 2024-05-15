#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnoDoNotDisposeNativeViews : DiagnosticAnalyzer
	{
		internal const string Title = "Do not dispose native views";
		internal const string MessageFormat = "Do not call Dispose() on {0}";
		internal const string Description = "Calling Dispose on native controls can cause undefined behaviors and runtime crashes.";
		internal const string Category = "Reliability";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
			"Uno0002",
#pragma warning restore RS2008 // Enable analyzer release tracking
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: Description,
			helpLinkUri: "https://aka.platform.uno/UNO0002"
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			// Debugger.Launch();

			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction(context =>
			{
				var uiViewSymbol = context.Compilation.GetTypeByMetadataName("UIKit.UIView");
				var nsObjectSymbol = context.Compilation.GetTypeByMetadataName("Foundation.NSObject");
				if (uiViewSymbol is null || nsObjectSymbol is null)
				{
					return;
				}

				context.RegisterOperationAction(
					c => AnalyzeOperation(c, uiViewSymbol, nsObjectSymbol)
					, OperationKind.Invocation
					, OperationKind.Using
					, OperationKind.UsingDeclaration
					, OperationKind.DeclarationPattern);
			});
		}

		private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol uiViewSymbol, INamedTypeSymbol nsObjectSymbol)
		{
			if (context.Operation is IInvocationOperation invocationOperation)
			{
				if (invocationOperation.TargetMethod.Name == "Dispose"
					&& IsType(invocationOperation.Instance?.Type, uiViewSymbol)
					&& (
						invocationOperation.TargetMethod.Parameters.Length == 0
						|| (
							invocationOperation.TargetMethod.Parameters.Length == 1
							&& invocationOperation.TargetMethod.Parameters[0].Type.SpecialType == SpecialType.System_Boolean)))
				{
					// We allow base.Dispose to be called from the Dispose method, as it means
					// it's the runtime that has called Dispose first for the current instance.
					var overridesDispose = context.ContainingSymbol is IMethodSymbol methodSymbol
						&& methodSymbol.Name == "Dispose"
						&& methodSymbol.IsOverride;

					if (!overridesDispose)
					{
						var diagnostic = Diagnostic.Create(
							Rule,
							context.Operation.Syntax.GetLocation(),
							invocationOperation.Instance?.Type?.ToDisplayString()
						);

						context.ReportDiagnostic(diagnostic);
					}
				}
			}
			else if (context.Operation is IUsingOperation usingOperation)
			{
				foreach (var local in usingOperation.Locals)
				{
					if (IsType(local.Type, uiViewSymbol))
					{
						var diagnostic = Diagnostic.Create(
						Rule,
						local.Locations.FirstOrDefault(),
						local.ToDisplayString()
					);

						context.ReportDiagnostic(diagnostic);
					}
				}
			}
			else if (context.Operation is IUsingDeclarationOperation usingDeclarationOperation)
			{
				foreach (var declarations in usingDeclarationOperation.DeclarationGroup.Declarations)
				{
					foreach (var declarator in declarations.Declarators)
					{
						if (IsType(declarator.Symbol.Type, uiViewSymbol))
						{
							var diagnostic = Diagnostic.Create(
								Rule,
								declarator.Syntax.GetLocation(),
								declarator.Symbol.Type?.ToDisplayString()
							);

							context.ReportDiagnostic(diagnostic);
						}
					}
				}
			}
		}

		private static bool IsType(ITypeSymbol? namedTypeSymbol, ISymbol? typeSymbol)
		{
			if (namedTypeSymbol != null)
			{
				if (typeSymbol is ITypeSymbol { SpecialType: SpecialType.System_Object })
				{
					// Everything is an object.
					return true;
				}

				do
				{
					if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol, typeSymbol))
					{
						return true;
					}

					namedTypeSymbol = namedTypeSymbol.BaseType;

					if (namedTypeSymbol == null)
					{
						break;
					}

				} while (namedTypeSymbol.SpecialType != SpecialType.System_Object);
			}

			return false;
		}
	}
}
