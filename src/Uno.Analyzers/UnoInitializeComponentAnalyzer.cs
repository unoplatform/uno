#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

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
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterCompilationStartAction(context =>
		{
			// .NET doesn't have ConcurrentHashSet. https://github.com/dotnet/runtime/issues/39919
			var xClasses = new ConcurrentDictionary<string, byte>();
			foreach (var xamlFile in context.Options.AdditionalFiles)
			{
				if (!xamlFile.Path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				var stream = new SourceTextStream(xamlFile.GetText(context.CancellationToken));

				using (XmlReader reader = XmlReader.Create(stream))
				{
					reader.MoveToContent();
					var xClass = reader.GetAttribute("x:Class");
					if (!string.IsNullOrEmpty(xClass))
					{
						xClasses.TryAdd(xClass, 0);
					}
				}
			}

			context.RegisterOperationAction(context =>
			{
				var invocation = (IInvocationOperation)context.Operation;
				if (invocation.TargetMethod.Name == "InitializeComponent" && invocation.TargetMethod.Parameters.Length == 0 &&
					context.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor && constructor.Parameters.Length == 0)
				{
					xClasses.TryRemove(context.ContainingSymbol.ContainingType.ToDisplayString(), out _);
				}
			}, OperationKind.Invocation);

			context.RegisterCompilationEndAction(context =>
			{
				foreach (var xClass in xClasses)
				{
					context.ReportDiagnostic(Diagnostic.Create(Rule, Location.None, xClass.Key));
				}
			});
		});
	}
}
