#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.WinAppSDK;

/// <summary>
/// Diagnostic descriptors for the WinAppSDK code-behind generator.
/// </summary>
internal static class XamlCodeBehindDiagnostics
{
	public static readonly DiagnosticDescriptor InvalidXClassRule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
		"UNOB0001",
#pragma warning restore RS2008 // Enable analyzer release tracking
		"Invalid x:Class Value",
		"{0}",
		"XAML",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "The x:Class attribute value is malformed and must include a namespace.");
}
