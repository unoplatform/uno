#nullable enable

using System;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.DependencyObject;

public partial class DependencyObjectGenerator
{
	private static readonly DiagnosticDescriptor _descriptor = new(
#pragma warning disable RS2008 // Enable analyzer release tracking
		id: "Uno0003",
#pragma warning restore RS2008 // Enable analyzer release tracking
		title: "Native view shouldn't implement 'DependencyObject'",
		messageFormat: "'{0}' shouldn't implement 'DependencyObject'. Inherit 'FrameworkElement' instead.",
		category: "Usage",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://github.com/unoplatform/uno/issues/6758#issuecomment-898544729",
		customTags: WellKnownDiagnosticTags.NotConfigurable);

	private static void ReportDiagnostic(GeneratorExecutionContext context, Diagnostic diagnostic)
	{
		context.ReportDiagnostic(diagnostic);
	}
}
