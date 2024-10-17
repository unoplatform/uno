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

		context.RegisterCompilationStartAction(context =>
		{
			var applicationSymbol = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.Application");
			var dependencyObjectSymbol = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.DependencyObject");
			if (applicationSymbol is null || dependencyObjectSymbol is null)
			{
				return;
			}

			context.RegisterSymbolStartAction(context =>
			{
				var type = (INamedTypeSymbol)context.Symbol;
				if (!TypeIsApplicationOrDependencyObject(type, applicationSymbol, dependencyObjectSymbol))
				{
					return;
				}

				IMethodSymbol? initializeComponent = null;
				foreach (var member in type.GetMembers("InitializeComponent"))
				{
					if (member is IMethodSymbol { Parameters.Length: 0 } method)
					{
						initializeComponent = method;
						break;
					}
				}

				if (initializeComponent?.Locations[0].SourceTree?.FilePath.Contains("XamlCodeGenerator") == true)
				{
					var isCalled = false;
					context.RegisterOperationAction(context =>
					{
						if (!isCalled &&
							initializeComponent.Equals(((IInvocationOperation)context.Operation).TargetMethod, SymbolEqualityComparer.Default) &&
							context.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor })
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
		});
	}

	private static bool TypeIsApplicationOrDependencyObject(INamedTypeSymbol type, INamedTypeSymbol applicationSymbol, INamedTypeSymbol dependencyObjectSymbol)
	{
		if (type.AllInterfaces.Contains(dependencyObjectSymbol))
		{
			return true;
		}

		var currentType = type;
		while (currentType is not null)
		{
			if (currentType.Equals(applicationSymbol, SymbolEqualityComparer.Default))
			{
				return true;
			}

			currentType = currentType.BaseType;
		}

		return false;
	}
}
