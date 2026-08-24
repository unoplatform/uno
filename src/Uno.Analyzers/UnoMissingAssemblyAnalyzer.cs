#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnoMissingAssemblyAnalyzer : DiagnosticAnalyzer
{
	internal const string Title = "An assembly required for a component is missing";
	internal const string MessageFormat = "Using '{0}' requires '{1}' NuGet package to be referenced";
	internal const string Category = "Correctness";

	internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
		"Uno0007",
#pragma warning restore RS2008 // Enable analyzer release tracking
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://aka.platform.uno/UNO0007"
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
	}
}
