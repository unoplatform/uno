#nullable enable

using System;
using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Uno.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnoXamlControlsResourcesAnalyzer : DiagnosticAnalyzer
{
	internal const string Title = "XamlControlsResources must be present in App.xaml";
	internal const string MessageFormat = "App.xaml is missing 'XamlControlsResources' in 'Application.Resources'. Most WinUI controls require it to be present.";
	internal const string Category = "Correctness";

	internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
		"Uno0008",
#pragma warning restore RS2008 // Enable analyzer release tracking
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://aka.platform.uno/UNO0008"
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterCompilationStartAction(compilationContext =>
		{
			// Only report if XamlControlsResources type is available (i.e., the Fluent theme is referenced)
			var xamlControlsResourcesType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.Controls.XamlControlsResources");
			if (xamlControlsResourcesType is null)
			{
				return;
			}

			compilationContext.RegisterAdditionalFileAction(fileContext =>
			{
				var file = fileContext.AdditionalFile;

				// Only process XAML files
				if (!file.Path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				var sourceText = file.GetText();
				if (sourceText is null)
				{
					return;
				}

				var content = sourceText.ToString();

				try
				{
					var xDoc = XDocument.Parse(content, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

					// Only check files whose root element is Application
					if (!string.Equals(xDoc.Root?.Name.LocalName, "Application", StringComparison.Ordinal))
					{
						return;
					}

					// Check if XamlControlsResources appears as any descendant element
					foreach (var element in xDoc.Descendants())
					{
						if (string.Equals(element.Name.LocalName, "XamlControlsResources", StringComparison.Ordinal))
						{
							return;
						}
					}

					// Point the diagnostic to the Application root element in the XAML file
					var location = GetRootElementLocation(xDoc, sourceText, file.Path);
					fileContext.ReportDiagnostic(Diagnostic.Create(Rule, location));
				}
				catch (XmlException)
				{
					// If the XAML cannot be parsed as XML, skip the check
				}
			});
		});
	}

	private static Location? GetRootElementLocation(XDocument xDoc, SourceText sourceText, string filePath)
	{
		if (xDoc.Root is not IXmlLineInfo lineInfo || !lineInfo.HasLineInfo())
		{
			return null;
		}

		var line = lineInfo.LineNumber - 1;
		var column = lineInfo.LinePosition - 1;

		if (line < 0 || line >= sourceText.Lines.Count)
		{
			return null;
		}

		var linePosition = new LinePosition(line, column);
		var textLine = sourceText.Lines[line];
		return Location.Create(filePath, textLine.Span, new LinePositionSpan(linePosition, linePosition));
	}
}
