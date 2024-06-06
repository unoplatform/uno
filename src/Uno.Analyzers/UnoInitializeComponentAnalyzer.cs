#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnoInitializeComponentAnalyzer : DiagnosticAnalyzer
{
	internal const string Title = "Call 'InitializeComponent()' from code-behind";
	internal const string MessageFormat = "Type '{0}' should call 'InitializeComponent()' from its constructor";
	internal const string Category = "Correctness";

	internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
		"Uno0006",
#pragma warning restore RS2008 // Enable analyzer release tracking
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://aka.platform.uno/UNO0006"
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterSymbolStartAction(context =>
		{
			var type = (INamedTypeSymbol)context.Symbol;
			IMethodSymbol? initializeComponent = null;
			foreach (var member in type.GetMembers("InitializeComponent"))
			{
				if (member is IMethodSymbol { Parameters.Length: 0 } method)
				{
					initializeComponent = method;
					break;
				}
			}

			if (initializeComponent is not null)
			{
				var isCalled = false;
				context.RegisterOperationAction(context =>
				{
					if (!isCalled &&
						initializeComponent.Equals(((IInvocationOperation)context.Operation).TargetMethod, SymbolEqualityComparer.Default) &&
						context.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor, Parameters.Length: 0 })
					{
						isCalled = true;
					}
				}, OperationKind.Invocation);

				context.RegisterSymbolEndAction(context =>
				{
					if (!isCalled)
					{
						context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.ToDisplayString()));
					}
				});
			}
		}, SymbolKind.NamedType);
	}
}
