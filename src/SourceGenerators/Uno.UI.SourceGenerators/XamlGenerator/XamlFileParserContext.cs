#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal class XamlFileParserContext(string file)
{
	private readonly List<XamlParsingException> _errors = new();
	private List<(string Prefix, string Namespace, int LineNumber, int LinePosition)>? _namespaceDeclarations;

	public void ReportError(string message, int lineNumber, int linePosition, Exception? inner = null)
		=> _errors.Add(new XamlParsingException(message, inner, lineNumber, linePosition, file));

	public void ReportError(DiagnosticDescriptor descriptor, string message, int lineNumber, int linePosition)
		=> _errors.Add(new XamlParsingException(message, null, lineNumber, linePosition, file, descriptor));

	/// <summary>
	/// Records a WPF-style <c>clr-namespace:</c> xmlns declaration, or one using a conditional prefix removed
	/// in 7.0. Validation is deferred to <see cref="ReportNamespaceDeclarations"/> because the
	/// <c>mc:Ignorable</c> prefixes are not known yet when the namespace node is visited.
	/// </summary>
	public void TrackNamespaceDeclaration(string prefix, string @namespace, int lineNumber, int linePosition)
		=> (_namespaceDeclarations ??= new()).Add((prefix, @namespace, lineNumber, linePosition));

	public bool HasNamespaceDeclarations => _namespaceDeclarations is not null;

	public void ReportNamespaceDeclarations(ICollection<string> ignorablePrefixes)
	{
		if (_namespaceDeclarations is null)
		{
			return;
		}

		foreach (var (prefix, @namespace, lineNumber, linePosition) in _namespaceDeclarations)
		{
			var isIgnorable = prefix.Length > 0 && ignorablePrefixes.Contains(prefix);

			if (XamlNamespaceValidation.IsClrNamespace(@namespace))
			{
				if (!isIgnorable)
				{
					ReportError(
						XamlCodeGenerationDiagnostics.UnsupportedClrNamespaceRule,
						XamlNamespaceValidation.FormatUnsupportedClrNamespaceMessage(prefix, @namespace),
						lineNumber,
						linePosition);
				}
			}
			else if (XamlNamespaceValidation.FormatRemovedConditionalPrefixMessage(prefix, @namespace, isIgnorable) is { } message)
			{
				ReportError(XamlCodeGenerationDiagnostics.RemovedConditionalXamlPrefixRule, message, lineNumber, linePosition);
			}
		}
	}

	public ImmutableArray<XamlParsingException> GetErrors()
		=> _errors.ToImmutableArray();
}
