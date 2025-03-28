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

		context.RegisterCompilationStartAction(context =>
		{
			var assemblies = context.Compilation.ReferencedAssemblyNames.Select(a => a.Name).ToImmutableHashSet();
			var progressRing = context.Compilation.GetTypeByMetadataName("Microsoft" /* UWP don't rename */ + ".UI.Xaml.Controls.ProgressRing");
			var mpe = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.Controls.MediaPlayerElement");
			_ = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.UnoRuntimeIdentifier", out var unoRuntimeIdentifier);
			_ = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.IsUnoHead", out var isUnoHead);
			if (isUnoHead is null || !isUnoHead.Equals("true", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			context.RegisterOperationAction(context =>
			{
				var objectCreation = (IObjectCreationOperation)context.Operation;
				if (objectCreation.Type is not INamedTypeSymbol type)
				{
					return;
				}

				if (type.DerivesFrom(progressRing) && !assemblies.Contains("Uno.UI.Lottie"))
				{
					const string lottieNuGetPackageName =
#if HAS_UNO_WINUI
						"Uno.WinUI.Lottie";
#else
						"Uno.UI.Lottie";
#endif

					context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.Syntax.GetLocation(), "ProgressRing", lottieNuGetPackageName));
				}
				else if (type.DerivesFrom(mpe))
				{
					if (unoRuntimeIdentifier?.Equals("WebAssembly", StringComparison.OrdinalIgnoreCase) == true)
					{
						if (!assemblies.Contains("Uno.UI.MediaPlayer.WebAssembly"))
						{
							const string wasmMPENuGetPackageName =
#if HAS_UNO_WINUI
								"Uno.WinUI.MediaPlayer.WebAssembly";
#else
								"Uno.UI.MediaPlayer.WebAssembly";
#endif
							context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.Syntax.GetLocation(), "MediaPlayer", wasmMPENuGetPackageName));
						}
					}
					else if (assemblies.Contains("Uno.UI.Runtime.Skia.Gtk"))
					{
						if (!assemblies.Contains("Uno.UI.MediaPlayer.Skia.Gtk"))
						{
							const string wasmMPENuGetPackageName =
#if HAS_UNO_WINUI
								"Uno.WinUI.MediaPlayer.Skia.Gtk";
#else
								"Uno.UI.MediaPlayer.Skia.Gtk";
#endif
							context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.Syntax.GetLocation(), "MediaPlayer", wasmMPENuGetPackageName));
						}
					}
				}
			}, OperationKind.ObjectCreation);
		});
	}
}
