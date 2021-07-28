using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	public static class XamlCodeGenerationDiagnostics
	{
		internal const string Title         = "XAML Generation Failed";
		internal const string MessageFormat = "{0}";
		internal const string Description   = "XAML Generation Failed";
		internal const string Category      = "XAML";

		public static readonly DiagnosticDescriptor GenericXamlErrorRule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
		                                                                                   "UXAML0001",
#pragma warning restore RS2008 // Enable analyzer release tracking
		                                                                                   Title,
		                                                                                   MessageFormat,
		                                                                                   Category,
		                                                                                   DiagnosticSeverity.Error,
		                                                                                   isEnabledByDefault: true,
		                                                                                   description: Description
		                                                                                  );

		public static readonly DiagnosticDescriptor GenericXamlWarningRule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
		                                                                                     "UXAML0002",
#pragma warning restore RS2008 // Enable analyzer release tracking
		                                                                                     Title,
		                                                                                     MessageFormat,
		                                                                                     Category,
		                                                                                     DiagnosticSeverity.Warning,
		                                                                                     isEnabledByDefault: true,
		                                                                                     description: Description
		                                                                                    );
	}
}
