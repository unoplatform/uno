#nullable enable
using System;
using Microsoft.CodeAnalysis;

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

	/// <summary>
	/// Optional diagnostic descriptor to use when this exception is reported, instead of the
	/// default <see cref="XamlCodeGenerationDiagnostics.GenericXamlErrorRule"/> (UXAML0001).
	/// Use this to surface specific WinUI-aligned diagnostic codes (e.g. XLS0501).
	/// </summary>
	public DiagnosticDescriptor? Descriptor { get; init; }

	/// <inheritdoc />
	public string FilePath => _location.FilePath;

	/// <inheritdoc />
	public int LineNumber => _location.LineNumber;

	/// <inheritdoc />
	public int LinePosition => _location.LinePosition;
}
