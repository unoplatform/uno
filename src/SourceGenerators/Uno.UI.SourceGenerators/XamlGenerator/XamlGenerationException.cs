#nullable enable
using System;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal class XamlGenerationException : Exception, IXamlLocation
{
	private readonly IXamlLocation _location;

	public XamlGenerationException(string message, IXamlLocation location)
		: base(message)
	{
		_location = location;
	}

	public XamlGenerationException(string message, Exception inner, IXamlLocation location)
		: base(message, inner)
	{
		_location = location;
	}

	/// <inheritdoc />
	public string FilePath => _location.FilePath;

	/// <inheritdoc />
	public int LineNumber => _location.LineNumber;

	/// <inheritdoc />
	public int LinePosition => _location.LinePosition;
}
