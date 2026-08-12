#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal class XamlFileParserContext(string file)
{
	private readonly List<XamlParsingException> _errors = new();
	private List<(string Prefix, string Namespace, int LineNumber, int LinePosition)>? _clrNamespaceDeclarations;

	public void ReportError(string message, int lineNumber, int linePosition, Exception? inner = null)
		=> _errors.Add(new XamlParsingException(message, inner, lineNumber, linePosition, file));

	public void ReportError(DiagnosticDescriptor descriptor, string message, int lineNumber, int linePosition)
		=> _errors.Add(new XamlParsingException(message, null, lineNumber, linePosition, file, descriptor));

	/// <summary>
	/// Records a WPF-style <c>clr-namespace:</c> xmlns declaration. Validation is deferred to
	/// <see cref="ReportUnsupportedClrNamespaces"/> because the <c>mc:Ignorable</c> prefixes are not
	/// known yet when the namespace node is visited.
	/// </summary>
	public void TrackClrNamespaceDeclaration(string prefix, string @namespace, int lineNumber, int linePosition)
		=> (_clrNamespaceDeclarations ??= new()).Add((prefix, @namespace, lineNumber, linePosition));

	public bool HasClrNamespaceDeclarations => _clrNamespaceDeclarations is not null;

	public void ReportUnsupportedClrNamespaces(ICollection<string> ignorablePrefixes)
	{
		if (_clrNamespaceDeclarations is null)
		{
			return;
		}

		foreach (var (prefix, @namespace, lineNumber, linePosition) in _clrNamespaceDeclarations)
		{
			if (prefix.Length > 0 && ignorablePrefixes.Contains(prefix))
			{
				continue;
			}

			ReportError(
				XamlCodeGenerationDiagnostics.UnsupportedClrNamespaceRule,
				XamlNamespaceValidation.FormatUnsupportedClrNamespaceMessage(prefix, @namespace),
				lineNumber,
				linePosition);
		}
	}

	public ImmutableArray<XamlParsingException> GetErrors()
		=> _errors.ToImmutableArray();
}
