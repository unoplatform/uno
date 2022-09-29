using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class WasmHttpHandlerDeprecatedAnalyzer : DiagnosticAnalyzer
	{
		internal const string Title = "'WasmHttpHandler' is deprecated";
		internal const string MessageFormat = "'WasmHttpHandler' is deprecated, use 'HttpClient' and 'HttpHandler' instead.";
		internal const string Category = "Usage";

		internal static DiagnosticDescriptor Rule = new(
			"Uno0002",
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction(context =>
			{
				var wasmHttpHandlerSymbol = context.Compilation.GetTypeByMetadataName("Uno.UI.Wasm.WasmHttpHandler");
				if (wasmHttpHandlerSymbol is null)
				{
					return;
				}

				context.RegisterOperationAction(c => AnalyzeOperation(c, wasmHttpHandlerSymbol), OperationKind.Invocation, OperationKind.ObjectCreation);
				context.RegisterSymbolAction(c => AnalyzeNamedType(c, wasmHttpHandlerSymbol), SymbolKind.NamedType);
			});
		}

		private void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol wasmHttpHandlerSymbol)
		{
			var namedType = (INamedTypeSymbol)context.Symbol;
			if (wasmHttpHandlerSymbol.Equals(namedType.BaseType))
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations.First()));
			}
		}

		private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol wasmHttpHandlerSymbol)
		{
			var symbol = GetTypeSymbolFromOperation(context.Operation);
			if (wasmHttpHandlerSymbol.Equals(symbol, SymbolEqualityComparer.Default))
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation()));
			}
		}

		private INamedTypeSymbol? GetTypeSymbolFromOperation(IOperation operation)
		{
			return operation switch
			{
				IInvocationOperation invocationOperation => invocationOperation.TargetMethod.ContainingType,
				IObjectCreationOperation objectCreation => objectCreation.Constructor.ContainingType,
				IFieldReferenceOperation fieldReferenceOperation => fieldReferenceOperation.Field.ContainingType,
				IPropertyReferenceOperation propertyReferenceOperation => propertyReferenceOperation.Property.ContainingType,
				_ => throw new InvalidOperationException("This code path is unreachable.")
			};
		}
	}
}
