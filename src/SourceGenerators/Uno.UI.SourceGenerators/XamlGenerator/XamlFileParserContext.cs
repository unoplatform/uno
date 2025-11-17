#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal class XamlFileParserContext(string file)
{
	private readonly List<XamlParsingException> _errors = new();

	public void ReportError(string message, int lineNumber, int linePosition, Exception? inner = null)
		=> _errors.Add(new XamlParsingException(message, inner, lineNumber, linePosition, file));

	public ImmutableArray<XamlParsingException> GetErrors()
		=> _errors.ToImmutableArray();
}
