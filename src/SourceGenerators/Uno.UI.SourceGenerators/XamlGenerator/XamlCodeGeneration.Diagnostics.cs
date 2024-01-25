using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator;

public static class XamlCodeGenerationDiagnostics
{
	internal const string Title = "XAML Generation Failed";
	internal const string MessageFormat = "{0}";
	internal const string XamlGenerationFailureDescription = "XAML Generation Failed";
	internal const string XamlCategory = "XAML";
	internal const string ResourcesCategory = "Resources";

#pragma warning disable RS2008 // Enable analyzer release tracking

	public static readonly DiagnosticDescriptor GenericXamlErrorRule = new DiagnosticDescriptor(
		"UXAML0001",
		Title,
		MessageFormat,
		XamlCategory,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: XamlGenerationFailureDescription
		);

	public static readonly DiagnosticDescriptor GenericXamlWarningRule = new DiagnosticDescriptor(
		"UXAML0002",
		Title,
		MessageFormat,
		XamlCategory,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: XamlGenerationFailureDescription
		);

	public static readonly DiagnosticDescriptor ResourceParsingFailureRule = new DiagnosticDescriptor(
		"UXAML0003",
		Title,
		MessageFormat,
		ResourcesCategory,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Resource Generation Failed"
		);

	public static readonly DiagnosticDescriptor ConditionalXamlInClassLibrary = new DiagnosticDescriptor(
		id: "UXAML0004",
		title: "Invalid use of XAML condition",
		messageFormat: "The conditional XAML namespace '{0}' cannot be used. File name: '{1}'.",
		category: XamlCategory,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: """
			When building a class library for net7.0 or net8.0, the reference binaries for Skia and Wasm are used at compile-time as the platform is not yet known.
			The class library build produces a single assembly that is used both for Skia and Wasm. So, Skia and Wasm XAML conditionals cannot be applied.
			In order to use Skia and Wasm conditions, you need to use a cross-runtime library.
			"""
		);
}
